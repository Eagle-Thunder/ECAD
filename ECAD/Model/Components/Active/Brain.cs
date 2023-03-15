namespace ECAD {
	internal abstract class Brain:Atom{
		protected override bool Process(bool flag){
			Pin? source=Pins.Get("source")?.Items[0];
			Pin? drain=Pins.Get("drain")?.Items[0];
			Pin? gate=Pins.Get("gate")?.Items[0];
			if(source is null||gate is null) return false;
			bool pinState=(flag)?gate.Active:!gate.Active;
			if(source.Pending||gate.Pending) drain?.Load(source.Active&&pinState);
			return true;}
		public Brain(string label,Component? owner=null,string type="",Behaviour b=Behaviour.Unset):base(owner,label,type,b){
			Pins.Items.Add(new Port("source",this,1,Direction.West,Behaviour.Input));
			Pins.Items.Add(new Port("gate",this,1,Direction.North,Behaviour.Input));
			Pins.Items.Add(new Port("drain",this,1,Direction.East,Behaviour.Output));}}}