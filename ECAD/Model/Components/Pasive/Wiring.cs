namespace ECAD {
	internal abstract class Wiring:Atom{
		protected override bool Process(bool flag){
			void UpdateOutputs(Collection<Port> port,bool[] state){
				for(int j=0;j<state.Length;j++) port.ForEach(port => port[j].Load(state[j]));}
			void ReadInputs(Collection<Port> port,ref bool[] state){
				for(int i=0;port.InRange(i);i++) for(int j=0;port[i].InRange(j);j++) state[j]=(port[i][j].Active)||state[j];}
			bool Filter(Port pin,Behaviour type,string mode="all"){
				return mode switch{
					"NS" => pin.Type==type&&(pin.side==Direction.North||pin.side==Direction.South),
					"EW" => pin.Type==type&&(pin.side==Direction.East ||pin.side==Direction.West),
					   _ => pin.Type==type};}
			bool Bridge(Collection<Port> port,bool[] state,string mode="all"){
				for(int k=0;k<state.Length;k++) state[k]=false;//init active inputs image
				port.Items=Pins.Where(p => Filter(p,Behaviour.Input,mode));ReadInputs(port,ref state);//record active inputs
				port.Items=Pins.Where(p => Filter(p,Behaviour.Output,mode));UpdateOutputs(port,state);//update outputs
				return true;}
			Collection<Port> pins=new();
			bool[] active=new bool[Pins[0].Count];
			return (flag)?Bridge(pins,active)//4-way bridge
						 :Bridge(pins,active,"NS")//NS bridge
						 &&Bridge(pins,active,"EW");/*EW bridge*/}
		public Wiring(string label,Component? owner=null,int width=1,string type="",Behaviour b=Behaviour.Unset):base(owner,label,type,b){
			Pins.Items.Add(new Port("WestIn",this,width,Direction.West,Behaviour.Input));
			Pins.Items.Add(new Port("NorthIn",this,width,Direction.North,Behaviour.Input));
			Pins.Items.Add(new Port("EastIn",this,width,Direction.East,Behaviour.Input));
			Pins.Items.Add(new Port("SouthIn",this,width,Direction.South,Behaviour.Input));
			Pins.Items.Add(new Port("WestOut",this,width,Direction.West,Behaviour.Output));
			Pins.Items.Add(new Port("NorthOut",this,width,Direction.North,Behaviour.Output));
			Pins.Items.Add(new Port("EastOut",this,width,Direction.East,Behaviour.Output));
			Pins.Items.Add(new Port("SouthOut",this,width,Direction.South,Behaviour.Output));}}}