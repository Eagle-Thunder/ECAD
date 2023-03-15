namespace ECAD {
	internal abstract class Atom:Component{
		public Behaviour behaviour{get;private set;}
		public Atom(Component? owner=null,string label="",string type="",Behaviour bh=Behaviour.Unset):base(label,type,owner) => behaviour=bh;
		protected abstract bool Process(bool flag);
		public override bool Run(){
			if(behaviour==Behaviour.Unset) return false;
			bool flag=behaviour==Behaviour.On||behaviour==Behaviour.Full;
			Process(flag);Pins.Run();return true;}}}