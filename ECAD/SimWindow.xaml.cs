using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ECAD {
	/// <summary> Interaction logic for SimWindow.xaml </summary>
	internal enum cips { If, GateSlice, Gate, Xor, FlipSlice, RsLatch, AdderCore, Bit, AdderSlice };
	public partial class SimWindow:Window {
		private Cip simCip;//cip on display. Will be modifieble through a 'load' action.
		private int zoom = 20;//display step. Will later be used for zooming
		private Port? Port = null;//needed to store link trigger during EditLinks mode (add/delete) and pin selection during EditPins mode (delete)
		private Component? Selection = null;//needed to edit components (move/delete)
		private ExecMode OpMode = ExecMode.Inspect;
		private void PickMode(object sender,SelectionChangedEventArgs e) {
			OpMode=((ComboBoxItem)(ModePick.SelectedItem)).Name switch {
				"EditPins" => ExecMode.EditPins,
				"EditComponents" => ExecMode.EditComponents,
				"EditLinks" => ExecMode.EditLinks,
				"Idle" => ExecMode.Inspect,
				"Running" => ExecMode.Running,
				_ => ExecMode.Inspect
			};
			if(OpMode==ExecMode.Running) RunCip(true);
		}
		private Cip LoadCip(cips c) {
			string s = Enum.GetName(c)??""; string n = s+"Sample";
			return ((int)c==2) ? new(n,s,null,new string[] { "8" }) : new(n,s);
		}
		public SimWindow() {//init window
			InitializeComponent();//incorporate xaml
								  //Canvas Drawing;<-- display area, set from xaml
								  //Grid InPins;<-- check boxes to set pin state, set from xaml
								  //ComboBox ModePick;<-- drop box topick working mode, set from xaml
								  //WrapPanel Tools;<-- container for command buttons, set from xaml
			simCip=LoadCip(cips.Bit);//hardcoded cip on display - will be loaded through a dialog later
			KeyUp+=EditComponent;//<--allows moving selected subchip arround when in the EditComponents mode, or deleting it.
			Loaded+=InitCip;/*regiesters event to show initial state od chip*/
		}
		private void InitCip(object sender,RoutedEventArgs e) => DisplayCip();// <-- should this unregister itsef?
		///<summary>Displays the chip loaded in the simulator</summary>
		private void DisplayCip() {//asta o sa fie pe un eveniment de click pe un buton de loadCip cind selectezi ceva din librarie
			int[] delta = { -1,0,0,0,0 };//number of pins on each side
			double[] options = { 0,0,0,0 };
			void InitCountMatrix(int i = 0) => delta[1]=delta[2]=delta[3]=delta[4]=i;
			void InitDisplay() {
				double px, py; InitCountMatrix();//reset the count matrix
				simCip.Pins.ForEach(pin => delta[(int)pin.side]++);//count pins per side
				options[(int)Cache.ParentHeight]=py=Drawing.ActualHeight;//get available vertical room
				options[(int)Cache.ParentWidth]=px=Drawing.ActualWidth;//get available horizontal room
				options[(int)Cache.StepX]=Math.Min(px/(delta[(int)Direction.North]+1),px/(delta[(int)Direction.South]+1));//get horizontal granulation
				options[(int)Cache.StepY]=Math.Min(py/(delta[(int)Direction.East]+1),py/(delta[(int)Direction.West]+1));/*get vertical granulation*/
			}
			void InitInputOptions() {//<--control for 'mode' already added from xaml
				void AddToggleControlForPin(Port inPin) {//<--needs some clutter management!
					void SetUpCheckBox(Pin pin,StackPanel panel,int ix) {
						CheckBox pinState = new();//create pin control
						pinState.Margin=new Thickness(5);
						pinState.Name=$"{pin.Parrent.Name}_{ix}";//name the control for referencing
						pinState.Unchecked+=UpdatePort; pinState.Checked+=UpdatePort;
						panel.Children.Add(pinState);/*register control*/
					}
					StackPanel ioPinState = new();//create port control (vertical by default)
					ioPinState.Margin=new Thickness(5,0,0,0);
					int i = 0;//init pin index
					TextBox textBox = new();
					textBox.Text=inPin.Name; textBox.Tag=inPin;
					ioPinState.Children.Add(textBox);//set display name for control
					inPin.ForEach(pin => SetUpCheckBox(pin,ioPinState,i++));
					InPins.Children.Add(ioPinState);
				}//display control
				List<Port> inputs = simCip.Pins.Where(pin => pin.Type==Behaviour.Input);
				inputs.ForEach(inPin => AddToggleControlForPin(inPin));
			}
			void GenerateDisplay() {
				Line AddPinLine(Port pin) {//creates a line for the given pin
					Line pinLine = new();
					pinLine.Tag=pin; pinLine.Name=pin.Name;
					pinLine.MouseUp+=EditCip;//<--this should link to 'EditCip' which will translate to 'AddLink' in the EditLinks mode or 'DeletePin' in EditPins mode
					pinLine.Stroke=(Port==pin) ? Brushes.Yellow :
								   (pin.Active) ? Brushes.Green : Brushes.Red;
					return pinLine;
				}
				void DisplayPin(Port pin) {
					void PickLineEndsByPinSide(Line pinLine) {//defines pin anchor
						switch(pin.side) {
							case Direction.North:
								pin.xPos=pinLine.X1=pinLine.X2=options[(int)Cache.StepX]*((++delta[(int)pin.side]));
								pin.yPos=pinLine.Y1=zoom; pinLine.Y2=0; break;
							case Direction.South:
								pin.xPos=pinLine.X1=pinLine.X2=options[(int)Cache.StepX]*((++delta[(int)pin.side]));
								pinLine.Y2=options[(int)Cache.ParentHeight]; pin.yPos=pinLine.Y1=pinLine.Y2-zoom; break;
							case Direction.West:
								pin.yPos=pinLine.Y1=pinLine.Y2=options[(int)Cache.StepY]*((++delta[(int)pin.side]));
								pin.xPos=pinLine.X1=zoom; pinLine.X2=0; break;
							case Direction.East:
								pin.yPos=pinLine.Y1=pinLine.Y2=options[(int)Cache.StepY]*((++delta[(int)pin.side]));
								pinLine.X2=options[(int)Cache.ParentWidth]; pin.xPos=pinLine.X1=pinLine.X2-zoom; break;
							default: break;
						}
					}
					Line pinLine = AddPinLine(pin);//add line for pin
					PickLineEndsByPinSide(pinLine);//position the line
					Drawing.Children.Add(pinLine);/*display pin*/
				}
				void DisplayComponent(Component cip) {
					void DisplayComponentPin(Component cip,Port pin,Canvas cipCanvas) {
						bool PickLineEndsByPinSide(Line pinLine) {
							bool PickForGeneralComponent(Line line) {
								switch(pin.side) {
									case Direction.North:
										line.X1=line.X2=zoom*delta[(int)pin.side];
										line.Y1=zoom; line.Y2=0; return true;
									case Direction.South:
										line.Y2=cipCanvas.Height; line.Y1=line.Y2-zoom;
										line.X1=line.X2=zoom*delta[(int)pin.side]; return true;
									case Direction.West:
										line.Y1=line.Y2=zoom*delta[(int)pin.side];
										line.X1=zoom; line.X2=0; return true;
									case Direction.East:
										line.X2=cipCanvas.Width; line.X1=line.X2-zoom;
										line.Y1=line.Y2=zoom*delta[(int)pin.side]; return true;
									default: return true;
								}
							}
							bool PickForSplitter(Line line,bool isCross = false) {
								int n = isCross ? zoom/2 : 0;
								switch(pin.side) {
									case Direction.North:
										line.Y1=cipCanvas.Height/2; line.Y2=line.Y1-zoom;
										line.X1=line.X2=cipCanvas.Width/2-n; return true;
									case Direction.South:
										line.Y1=cipCanvas.Height/2; line.Y2=line.Y1+zoom;
										line.X1=line.X2=cipCanvas.Width/2-n; return true;
									case Direction.West:
										line.X1=cipCanvas.Width/2; line.X2=line.X1-zoom;
										line.Y1=line.Y2=cipCanvas.Height/2-n; return true;
									case Direction.East:
										line.X1=cipCanvas.Width/2; line.X2=line.X1+zoom;
										line.Y1=line.Y2=cipCanvas.Height/2-n; return true;
									default: return true;
								}
							}
							return (cip.Type=="Splitter"||cip.Type=="Cross") ? PickForSplitter(pinLine,cip.Type=="Cross") : PickForGeneralComponent(pinLine);
						}
						Line pinLine = AddPinLine(pin);//add line for pin, extracted 2 function
						PickLineEndsByPinSide(pinLine);//position the line
						if(cip.Type!="Splitter") delta[(int)pin.side]++;//count pin in count matrix
						pin.xPos=pinLine.X2+zoom*cip.xPos; pin.yPos=pinLine.Y2+zoom*cip.yPos;//define pin anchor
						if(pin.Linked) cipCanvas.Children.Add(pinLine);/*display pin*/
					}//<--condition should only be checked in running mode
					void Position<T>(T control,double x,double y) where T : DependencyObject {
						control.SetValue(Canvas.LeftProperty,x); control.SetValue(Canvas.TopProperty,y);
					}
					void SetSize<T>(T control,double w,double h) where T : FrameworkElement {
						control.Height=h; control.Width=w;
					}
					Canvas InitComponentCanvas(Component chip) {
						Canvas cipCanvas = new(); cipCanvas.MouseUp+=EditCip;//<--this should link to 'EditCip' that becomes 'SelectComponent' in EditComponents mode
																			 //^^^ must check that the pin lines within the canvas still respond to clicks propperly
						SetSize(cipCanvas,chip.xSize()*zoom,chip.ySize()*zoom);//size the canvas
						if(cip.Type=="Splitter") { cipCanvas.Height-=zoom; cipCanvas.Width-=zoom; }
						Position(cipCanvas,zoom*chip.xPos,zoom*chip.yPos);//position the canvas
						return cipCanvas;
					}
					TextBlock InitComponentDescription(Component chip) {
						TextBlock cipName = new(); string[] cnp = chip.Name.Split('.');
						cipName.Text=cnp[0]+((cnp.Length>1&&cnp[1]!="0") ? cnp[1] : "");
						int offset = (chip.Type.Contains("Brain")) ? 0 : 1;
						Position(cipName,zoom+1.0,zoom*offset+1.0);//position the descriptor
						return cipName;
					}
					Shape InitComponentLogo(Canvas canvas) {
						Shape AdjustSquareLogo(Shape box,string type) {//make a splitter look smaller, rotated and filled
							box.Stroke=cip.Pins.Any(p => p.Active) ? Brushes.Green : Brushes.Red;
							TransformGroup diamond = new();
							if(type=="Splitter") {
								RotateTransform Rot45 = new(45);
								Rot45.CenterX=box.Width/2; Rot45.CenterY=box.Height/2;
								diamond.Children.Add(Rot45);
							}
							ScaleTransform Scl45 = new(); Scl45.ScaleX=Scl45.ScaleY=1/Math.Sqrt(2)/4;
							Scl45.CenterX=box.Width/2; Scl45.CenterY=box.Height/2;
							diamond.Children.Add(Scl45); box.StrokeThickness=5;
							box.RenderTransform=diamond; return box;
						}
						Shape AdjustRoundLogo(Shape box,string type,Component cip,Canvas c) {
							int FlipPinLine(Port pin,bool vert = false) {
								return pin.side switch {
									Direction.North => vert ? 1 : 0,
									Direction.East => vert ? 0 : -1,
									Direction.South => vert ? -1 : 0,
									Direction.West => vert ? 0 : 1,
									_ => 0
								};
							}
							Line SetupStroke(Port? pin,Canvas canvas) {
								Line what = new();
								what.X1=pin.xPos-Canvas.GetLeft(canvas)+FlipPinLine(pin)*zoom;
								what.Y1=pin.yPos-Canvas.GetTop(canvas)+FlipPinLine(pin,true)*zoom;
								what.X2=canvas.Width/2; what.Y2=canvas.Height/2;
								what.Stroke=(pin.Active) ? Brushes.Green : Brushes.Red;
								what.StrokeDashArray=DoubleCollection.Parse("5,1,3,1,5,2,3,1");
								return what;
							}
							c.Children.Add(SetupStroke(cip.Pins.Get("source"),c));
							c.Children.Add(SetupStroke(cip.Pins.Get("gate"),c));
							c.Children.Add(SetupStroke(cip.Pins.Get("drain"),c));
							if(cip.Type=="OffBrain") box.StrokeDashArray=DoubleCollection.Parse("5,3");
							return box;
						}
						Shape cipBox = cip.Type switch {
							"OffBrain" => new Ellipse(),
							"Splitter" => new Rectangle(),
							"OnBrain" => new Ellipse(),
							"Cross" => new Rectangle(),
							_ => new Rectangle()
						};
						cipBox.Stroke=Brushes.Black; int i = (cip.Type=="Cross") ? 3 : 2;
						SetSize(cipBox,canvas.Width-i*zoom,canvas.Height-i*zoom);//size the box
						Position(cipBox,zoom*1.0,zoom*1.0);//position the box
						return cip.Type switch {
							"Cross" => AdjustSquareLogo(cipBox,"Cross"),
							"Splitter" => AdjustSquareLogo(cipBox,"Splitter"),
							"OffBrain" => AdjustRoundLogo(cipBox,"OffBrain",cip,canvas),
							"OnBrain" => AdjustRoundLogo(cipBox,"OnBrain",cip,canvas),
							_ => cipBox
						};
					}
					Canvas cipCanvas = InitComponentCanvas(cip);//add cip canvas
					InitCountMatrix(2);//reset count matrix
					cip.Pins.ForEach(pin => DisplayComponentPin(cip,pin,cipCanvas));//add cip pins
					cipCanvas.Children.Add(InitComponentLogo(cipCanvas));//add cip box
					if(cip.Type!="Splitter"&&cip.Type!="Cross") cipCanvas.Children.Add(InitComponentDescription(cip));//add cip descriptor
					Drawing.Children.Add(cipCanvas);/*display cip*/
				}
				void DisplayLink(Port trigger) {
					Line AddLinkLine(Pin pin,int i) {
						Line line = new(); line.Tag=pin.Parrent; line.Name=$"Link_{pin.Parrent.Name}_{i}";
						line.MouseUp+=EditCip;//<--this should link to 'EditCip' which will translate to 'DeleteLink' in the EditLinks mode
						line.Stroke=(pin.Active) ? Brushes.Green : Brushes.Red;
						return line;
					}
					void AnchorLink(Line line,Pin pin) {
						line.X1=pin.Parrent.xPos; line.X2=pin.Link?.Parrent.xPos??0;
						line.Y1=pin.Parrent.yPos; line.Y2=pin.Link?.Parrent.yPos??0;
					}
					Line lastLine = null;
					if(!trigger.Linked) return;//temp - do not display unlinked pins
					for(int i = 0;i<trigger.Items.Count;i++) {
						Line linkLine = AddLinkLine(trigger[i],i);//add line for link
						AnchorLink(linkLine,trigger[i]);//position the link
						if(lastLine is not null&&lastLine.X1==linkLine.X1&&lastLine.Y1==linkLine.Y1
							&&lastLine.X2==linkLine.X2&&lastLine.Y2==linkLine.Y2) continue;
						lastLine=linkLine;
						Drawing.Children.Add(linkLine);/*display line*/
					}
				}
				void DrawFrame() {
					Line CipBorder(string side,int x1,int y1,int x2,int y2) {
						Line line = new(); line.MouseUp+=EditCip; line.Stroke=Brushes.Black;
						line.X1=x1; line.Y1=y1; line.X2=x2; line.Y2=y2; line.Name=$"Border_{side}";
						return line;
					}
					Drawing.Children.Add(CipBorder("Left",
						zoom/2,zoom/2,
						zoom/2,(int)options[(int)Cache.ParentHeight]-zoom/2));
					Drawing.Children.Add(CipBorder("Right",
						(int)options[(int)Cache.ParentWidth]-zoom/2,zoom/2,
						(int)options[(int)Cache.ParentWidth]-zoom/2,(int)options[(int)Cache.ParentHeight]-zoom/2));
					Drawing.Children.Add(CipBorder("Up",
						zoom/2,zoom/2,
						(int)options[(int)Cache.ParentWidth]-zoom/2,zoom/2));
					Drawing.Children.Add(CipBorder("Down",
						zoom/2,(int)options[(int)Cache.ParentHeight]-zoom/2,
						(int)options[(int)Cache.ParentWidth]-zoom/2,(int)options[(int)Cache.ParentHeight]-zoom/2));
				}
				simCip.Cips.ForEach(cip => DisplayComponent(cip));//dispaly cips
				InitCountMatrix();//reset the count matrix
				DrawFrame();//display border
				simCip.Pins.ForEach(pin => DisplayPin(pin));//display pins
				var LinkedPins = simCip.Pins.Where(pin => pin[0].Link!=null);//get linked cip pins
				simCip.Cips.ForEach(cip => LinkedPins.AddRange(cip.Pins.Where(pin => pin.LinkedFw&&pin.Type==Behaviour.Output)));//add linked subcip pins
				LinkedPins.ForEach(pin => DisplayLink(pin));/*display links*/
			}
			Drawing.Children.Clear();//ready canvas for drawing
			InitDisplay();//set caching options
			if(InPins.Children.Count==0) InitInputOptions();
			GenerateDisplay();/*then do the display*/
		}
		///<summary>Adds a link to the curent chip design</summary>
		private void AddLink(object sender,MouseButtonEventArgs e) {//<--addition to permit link editting
			Port pin = (Port)((Line)sender).Tag;//get the related port
			pin=(pin.Parrent.Type=="Splitter") ? (Port is null)//select correct pin for splitter
				? pin.Parrent.Pins.First(p => p.side==pin.side&&p.Type==Behaviour.Input) //get the input port to link to
				: pin.Parrent.Pins.First(p => p.side==pin.side&&p.Type==Behaviour.Output)//get the output port to link from
				: pin;//component is not a splitter
			if(pin.Parrent!=simCip&&pin.Type!=Behaviour.Output) return;//can only link from output pins of subcips
			if(pin.Parrent==simCip&&pin.Type==Behaviour.Output) return;//can only link from input pins of this chip
			if(Port is null) Port=pin;//register link start
			else {
				if(Port==pin) Port=null;//abort registration
				else Port.LinkTo(pin);/*register link end*/
			}
			DisplayCip();
		}
		///<summary>Deletes the link clicked on (no confirmation required)</summary>
		private void DeleteLink(object sender,MouseButtonEventArgs e) {//<--addition to permit link editting
			((Port)((Line)sender).Tag).LinkTo(null);//deregister link
			DisplayCip();
		}
		///<summary>Adds a pin to the curent chip design</summary>
		private void AddPin(object sender,MouseButtonEventArgs e) {//adds a default port of width 1 and no params set that must be configgured before use in inspect mode
			string side = ((Line)sender).Name.Split('_')[1];
			Port pin = new("UnnamedPin",simCip);//setting name,count and IOtype is delegated to the details panel (must be done before linking)
			pin.side=side switch {
				"Left" => Direction.West,
				"Right" => Direction.East,
				"Down" => Direction.South,
				"Up" => Direction.North,
				_ => Direction.Unset
			};
			simCip.Pins.Items.Add(pin);
			DisplayCip();
		}
		///<summary>Deletes the selected pin and associated link</summary>
		private void DeletePin(object sender,MouseButtonEventArgs e) {
			Port pin = (Port)((Line)sender).Tag;
			if(pin.Linked) {
				bool fwLink = pin.Items[0].Linked==Direction.Forward;
				if(fwLink) pin.LinkTo(null); else pin.Items[0]?.Link?.Parrent.LinkTo(null);
			}
			simCip.Pins.Items.Remove(pin);
		}
		///<summary>Adds a subchip to the curent chip design</summary>
		private void AddComponent(object sender,MouseButtonEventArgs e) {//adds a splitter of width 1 which may be changed in inspect mode
			Component cip = new Splitter("UnnamedSplitter");
			cip.xPos=cip.yPos=5;
			simCip.Cips.Items.Add(cip);
			DisplayCip();
		}
		///<summary>Selects the selected subchip for movement or deletion</summary>
		private void SelectComponent(object sender,MouseButtonEventArgs e) {
			Selection=(Component)((Line)sender).Tag;
			DisplayCip();
		}
		///<summary>Deletes the selected subchip and assocated links, or moves it</summary>
		private void EditComponent(object sender,KeyEventArgs e) {
			if(Selection is null) return;
			switch(e.Key) {
				case Key.Delete: simCip.Cips.Items.Remove(Selection); Selection=null; break;
				case Key.Left: Selection.xPos--; break;
				case Key.Right: Selection.xPos++; break;
				case Key.Down: Selection.yPos++; break;
				case Key.Up: Selection.yPos--; break;
				case Key.Enter: Selection=null; break;
				default: break;
			}
			DisplayCip();
		}
		///<summary>Adds something to, or deletes something from, the curent chip design based on "mode"</summary>
		private void EditCip(object sender,MouseButtonEventArgs e) {//selects an Add* or a Delete* operation through a switch on "mode"
			if(sender.GetType()==typeof(Line)) switch(OpMode) {
					case ExecMode.EditComponents:
						if(((Line)sender).Name.Contains("Cip")) SelectComponent(sender,e);
						else AddComponent(sender,e); break;
					case ExecMode.EditPins:
						if(((Line)sender).Name.Contains("Border")) AddPin(sender,e);
						else DeletePin(sender,e); break;
					case ExecMode.EditLinks:
						if(((Line)sender).Name.Contains("Link")) DeleteLink(sender,e);
						else AddLink(sender,e); break;
					case ExecMode.Inspect:/*must update details panel with sender*/break;
					default: break;
				}
		}
		///<summary>Runs a simulation step (when mode is set to running)</summary>
		private void UpdatePins(StackPanel pinState) {//<--this should be called when "mode" becomes Mode.Running
			List<CheckBox> states = new(); Port? inPin = null;
			void parseInputs(object control) {
				if(control.GetType()==typeof(TextBox)) inPin=(Port)((TextBox)control).Tag;//get the related port
				else states.Add((CheckBox)control);/*get the pin states for the related port*/
			}
			void UpdatePin(int i) {
				bool state = states.First(p => p.Name.Contains($"{i}")).IsChecked??false;
				inPin[i].Load(state);
			}
			foreach(var child in pinState.Children) parseInputs(child);//parse the control
			for(int i = 0;inPin?.InRange(i)??false;i++) UpdatePin(i);/*traverse the port and update pin*/
		}
		private void RunCip(bool refresh) {//asta trebuie sa se asocieze cu toate check boxurile generate...
			if(refresh) foreach(StackPanel ioPinState in InPins.Children) UpdatePins(ioPinState);//process toggle controls
			if(OpMode!=ExecMode.Running) return;
			simCip.Run(); DisplayCip();
		}
		private void UpdatePort(object sender,RoutedEventArgs e) {
			UpdatePins((StackPanel)((CheckBox)sender).Parent);
			RunCip(false);
		}
	}
}
