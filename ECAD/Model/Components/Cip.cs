//{dependencies
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
//}

namespace ECAD {
	internal class Cip:Component{
		public Collection<Component> Cips{get;private set;}
		private readonly Dictionary<string,string> param;
		public Cip(string label,string Cdf,Component? owner=null,string[]? Options=null):base(label,Cdf,owner){//<-- should this catch exceptions from PFL?
			Cips=new("Subchips");	Load status=Load.Undefined;
			param=new();			ErrorManager error=new();
			bool ProcessFileLine(string line){//parse config file
				bool LoadConfig(){
					string[] keys=line.Split(',');
					for(int i=0;i<keys.Length;i++) param[keys[i]]=(Options is null)?"0":Options[i];
					return true;}
				bool LoadPin(){
					string[] lineData=line.Split('@');//parse the data line
					string[] pinData=lineData[0].Split('-');
					string[] pinData1=pinData[0].Split('(');
					Direction dir=lineData[1] switch{//find pin direction
						"W"=>Direction.West,	"E"=>Direction.East,
						"N"=>Direction.North,	"S"=>Direction.South,
						  _=>Direction.Unset};
					Behaviour pioMode=pinData[1] switch{//determine pin mode
						 "In"=>Behaviour.Input,	"Out"=>Behaviour.Output,
							_=>Behaviour.Unset};
					int count=1;
					if(pinData1.Length>1) _=int.TryParse(param[pinData1[1].TrimEnd(')')],out count);
					Pins.Items.Add(new Port(pinData1[0],this,count,dir,pioMode));
					return true;}
				bool LoadCip(){
					string[] lineData=line.Split('@');
					string[]? SetCipParams(string? cfgData){
						if(cfgData is null||cfgData.Length<=1) return null;
						string[] ret=cfgData.TrimEnd(')').Split(',');
						for(int i=0;i<ret.Length;i++) if(param.ContainsKey(ret[i])) ret[i]=param[ret[i]];
						return ret;}
					Dictionary<string,int> SetCipArray(string[] cfgData){
						Dictionary<string,int> ret=new();
						ret["ArraySize"]=1;ret["ArrayStep"]=0;
						if(cfgData.Length<2) return ret;
						string cfg=cfgData[1].TrimEnd(']');
						string[] split7=cfg.Split(',');
						_=int.TryParse(param[split7[0]],out int i);ret["ArraySize"]=i;
						if(split7.Length>1){_=int.TryParse(param[split7[1]],out i);ret["ArrayStep"]=i;}
						return ret;}
					void RegisterCip(Dictionary<string,int> ca,Dictionary<string,string> cd,string[]? co){
						Component InitCip(Dictionary<string,int> ca,Dictionary<string,string> Data,string[]? Options){
							Component cip=null;int flipped;
							string sn=$".{ca["ArraySize"]-1}";
							try{ cip=Data["Type"] switch{
								 "OnBrain"=>new OnBrain(Data["Name"]+sn,this),
								"OffBrain"=>new OffBrain(Data["Name"]+sn,this),
								"Splitter"=>new Splitter(Data["Name"]+sn,this),
								   "Cross"=>new Cross(Data["Name"]+sn,this),
								_=>new Cip(Data["Name"]+sn,Data["Type"],this,Options)};}
							catch(Exception ex){error.ChainThrow("CannotLoadComponent",ex,lineData[0]);}
							//position the chip
							if(double.TryParse(Data["xPos"],out double d)) cip.xPos=d;
							else error.Throw("BadHorizontalPos",lineData[0]);
							if(double.TryParse(Data["yPos"],out d)) cip.yPos=d;
							else error.Throw("BadVerticalPos",lineData[0]);
							if(int.TryParse(Data["Rotation"],out int rot)) while(rot-->0) cip.Rotate();//rotate the chip
							else error.Throw("BadRotation",lineData[0]);
							flipped=Data["Mirrored"] switch{//flip the chip
								"flipH"=>cip.hFlip(),		"flipV"=>cip.vFlip(),
								"flipHV"=>cip.hFlip()+cip.vFlip(),_=>0};
							return cip;}
						string? dir=(cd["xPos"].Contains('+'))?"xPos":(cd["yPos"].Contains('+'))?"yPos":null;
						if(dir is not null) cd[dir]=cd[dir].Remove(cd[dir].IndexOf('+'));
						Component cip=InitCip(ca,cd,co);//init the chip
						Cips.Items.Add(cip);//register the chip
						//update position for next copy (depending on tilling)
							ca["ArraySize"]--;
							if(dir is null) return;
							int cipside=(dir=="xPos")?cip.xSize():(dir=="yPos")?cip.ySize():-1;
							_=int.TryParse(cd[dir],out int size);
							cd[dir]=$"{size+cipside+ca["ArrayStep"]}+0";}
					//parse the data line
					string[] split1=lineData[0].Split(':');		string[] split2=lineData[1].Split('&');
					string[] split3=split2[0].Split(',');		string[] split4=split2[1].Split('^');
					string[] split5=split1[1].Split('(');		string[] split6=split1[0].Split('[');
					string? CipParam=(split5.Length>1)?split5[1]:null;
					string[]? CipOptions=SetCipParams(CipParam);
					Dictionary<string,string> CipData=new();
					Dictionary<string,int> CipArray=SetCipArray(split6);
					CipData["Name"]=split6[0];		CipData["Type"]=split5[0];
					CipData["xPos"]=split3[0];		CipData["yPos"]=split3[1];
					CipData["Rotation"]=split4[0];	CipData["Mirrored"]=split4[1];
					while(CipArray["ArraySize"]>0) RegisterCip(CipArray,CipData,CipOptions);
					return true;}
				bool LoadLink(){
					Dictionary<string,string> SetLinkParam(string[] source){
						Dictionary<string,string> result=new();
						if(source.Length>1){//parse parameter, if it exist
							string[] linkparam=source[1].Split(':');//find name
							string[] paramdata=linkparam[1].Split('|');//find limits
							result["param"]=linkparam[0];//record param name
							result["pmin"]=paramdata[0];//record param start value
							result["pmax"]=paramdata[1];}//record param cap (not reached)
						return result;}
					void SetLinkData(string what,string datain,Dictionary<string,string> dataout){
						string[] s1;string[] s2;string s;
						if(datain.Contains('.')){
							s1=datain.Split('.');
							if(s1[0].Contains('[')){
								s2=s1[0].Split('[');
								dataout[what+"CipName"]=s2[0];
								dataout[what+"CipParam"]=s2[1].TrimEnd(']');}
							else dataout[what+"CipName"]=s1[0];
							s=s1[1];}
						else s=datain;
						if(s.Contains('(')){
							s1=s.Split('(');
							dataout[what+"PinName"]=s1[0];
							dataout[what+"PinParam"]=s1[1].TrimEnd(')');}
						else dataout[what+"PinName"]=s;}
					List<Pin> SetLinkEndpoint(string what,Dictionary<string,string> Data,int width=0){
						void SetParamRange(string what,string where,Dictionary<string,string> Data,out int min,out int max){
							int Eval(string what,bool lookup=false){
								int LookUp(string what){
									string source=(param.ContainsKey(what))?param[what]:what;
									_=int.TryParse(source,out int ret);
									return ret;}
								char c=(what.Contains('+'))?'+':(what.Contains('-'))?'-':'#';
								string[] s=what.Split(c);
								string s1=s[0];
								string s2=(s.Length>1)?s[1]:"0";
								int i0=(lookup)?LookUp(s1):Eval(s1,true);
								int i1=(lookup)?LookUp(s2):Eval(s2,true);
								return (what.Contains('-'))?i0-i1:i0+i1;}
							int Compute(string what,int param,Dictionary<string,string> Data){
								char c=(what.Contains('+'))?'+':(what.Contains('-'))?'-':'#';
								string[] sp=what.Split(c);
								string s=(sp.Length>1)?sp[1]:"0";
								_=int.TryParse(s,out int n);
								return (what.Contains('-'))?param-n:param+n;}
							min=0;max=1;
							if(Data.ContainsKey(what+where+"Param")&&
							   Data[what+where+"Param"].Contains(Data["param"])){
								min=Compute(Data[what+where+"Param"],Eval(Data["pmin"]),Data);
								max=Compute(Data[what+where+"Param"],Eval(Data["pmax"]),Data);}}
						List<Component> FindEnpointComponents(string what,Dictionary<string,string> Data){
							void SelectComponent(int i,List<Component> destination,Dictionary<string,string> Data){
								Component? comp=Cips.FirstOrDefault(c => c.Name==Data[what+"CipName"]+$".{i}");
								if(comp is null) error.Throw("CipNotFound",line,Data[what+"CipName"]+$".{i}");
								else destination.Add(comp);}
							List<Component> result=new();
							SetParamRange(what,"Cip",Data,out int i0,out int i1);
							if(!Data.ContainsKey(what+"CipName")) result.Add(this);
							else for(int i=i0;i<i1;i++) SelectComponent(i,result,Data);
							return result;}
						List<Pin> SelectEndpointPins(string what,List<Component> where,Dictionary<string,string> Data){
							void ParseSelection(int i,string what,List<Component> from,Dictionary<string,string> Data,List<Pin> into){
								void ValidatePort(Port? port,string endpoint,Dictionary<string,string> Data){
									Behaviour t1=(endpoint=="Trigger")?Behaviour.Output:Behaviour.Input;
									Behaviour t2=(endpoint=="Trigger")?Behaviour.Input:Behaviour.Output;
									string s1=(endpoint=="Trigger")?"Out":"In";
									string s2=(endpoint=="Trigger")?"In":"Out";
									string err1=$"Link{endpoint}Not{s1}put";
									string err2=$"Link{endpoint}Not{s2}put";
									if(Data.ContainsKey(endpoint+"CipName")&&port?.Type!=t1)
										error.Throw(err1,line,Data[endpoint+"PinName"]);
									if(!Data.ContainsKey(endpoint+"CipName")&&port?.Type!=t2)
										error.Throw(err2,line,Data[endpoint+"PinName"]);}
								Port? pin=from[i].Pins.Get(Data[what+"PinName"]);
								if(pin is null) error.Throw("PinNotFound",line,Data[what+"PinName"]);
								ValidatePort(pin,what,Data);
								SetParamRange(what,"Pin",Data,out int i0,out int i1);
								for(int j=i0;j<i1;j++){
									if(j<0||j>pin?.Count) error.Throw("PinNotFound",line,Data[what+"PinName"]+$"{j}");
									into.Add(pin[j]);}}
							List<Pin> result=new();
							for(int i=0;i<where.Count;i++) ParseSelection(i,what,where,Data,result);
							return result;}
						if(Data["TargetPinName"]=="^"){
							List<Pin> result=new();
							while(width-->0) result.Add(null);
							return result;}
						List<Component> selection=FindEnpointComponents(what,Data);
						return SelectEndpointPins(what,selection,Data);}
					string[] lineData=line.Split(',');//parse the data line
					string[] linkdata=lineData[0].Split('>');//parse link ends
					Dictionary<string,string> Ldt=SetLinkParam(lineData);//find link parameters
					SetLinkData("Trigger",linkdata[0],Ldt);//compute trigger requirements
					SetLinkData("Target",linkdata[1],Ldt);//compute target requirements
					List<Pin> trigger=SetLinkEndpoint("Trigger",Ldt);//find trigger pins
					List<Pin> target=SetLinkEndpoint("Target",Ldt,trigger.Count);//find target pins
					if(trigger.Count!=target.Count) error.Throw("LinkWidthNotConstant",line,$"{trigger.Count}!={target.Count}");
					for(int i=0;i<trigger.Count;i++) trigger[i].LinkTo(target[i],Direction.Forward);//register the link
					return true;}
				bool processed=false;
				if(line.Length==0||line[0]=='#') return processed;//ignore comments
				if(line.Contains('#')) line=line.Split('#')[0];//trim trailing commets
				if(line[0]=='[') status=line switch{//choose section to load
					"[Pins]"=>Load.Pins,		"[Links]" =>Load.Links,
					"[Cips]"=>Load.Cips,		"[Config]"=>Load.Config,
						   _=>Load.Undefined};
				else processed=status switch{//load an item from current section
					Load.Pins=>LoadPin(),		Load.Links =>LoadLink(),
					Load.Cips=>LoadCip(),		Load.Config=>LoadConfig(),
							_=>false};
				return processed;}
			string[]? readText=null;
			try{	readText=File.ReadAllLines($".\\Resources\\{Cdf}.cip");
					try{	readText?.ToList().ForEach(line => ProcessFileLine(line));}
					catch(Exception ex){error.ChainThrow("TemplateFileParsingError",ex);}}
			catch(Exception ex){error.ChainThrow("CannotReadTemplateFile",ex,Cdf);}}
		public override bool Run(){
			Collection<Component> Changed=new();
			Collection<Port> PendingPins=new();
			if(!Pins.Run()) return false;
			do{	Changed.Items=Cips.Where(cip => cip.Pending);
				if(Changed.Empty)  return true;
				if(!Changed.Run()) return false;
				foreach(var comp in Changed.Items){
					List<Port> PendingOut=comp.Pins.Where(ip => ip.Pending&&ip.Type==Behaviour.Output);
					PendingPins.Items.AddRange(PendingOut);}
				PendingPins.Run();}
			while(true);}}}