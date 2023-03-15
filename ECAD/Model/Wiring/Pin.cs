namespace ECAD {
	internal class Pin:Clocked{
		private int ix;
		public bool Active{get;private set;}
		public bool Pending{get;private set;}
		public Port Parrent{get;private set;}
		public Pin? Link{get;private set;}
		public Direction Linked{get;private set;}
		public Pin(Port p,int n=0){Parrent=p;Active=Pending=false;Link=null;ix=n;}
		public override string ToString() => Parrent.Name+$"_{ix}";
		private bool Set(){if(!Active)Active=Pending=true;return true;}
		private bool Reset(){if(Active){Active=false;Pending=true;}return true;}
		public bool Load(bool Cmd) => Cmd?Set():Reset();
		public void LinkTo(Pin? what,Direction where=Direction.Unset){
			bool _Unlink(Direction where){
				if(Link is null) return false;
				switch(where){
					case Direction.Unset:return false;
					case Direction.Forward:Link.LinkTo(null,Direction.Backward);break;
					case Direction.Backward:if(Linked!=Direction.Backward) return false;else break;
					default:return false;}
				Linked=Direction.Unset;
				Link=null; return true;}
			bool _Link(Pin what,Direction where){
				Direction dir=where switch{
					Direction.Forward =>Direction.Backward,
					Direction.Backward=>Direction.Forward,
									 _=>Direction.Unset};
				switch(where){
					case Direction.Unset:return false;
					case Direction.Forward:
						if(Link is not null){
							if(what==Link) return false;
							if(Linked==Direction.Forward) Link.LinkTo(null,dir);}
					break;case Direction.Backward:
						if(Link is not null){
							if(what==Link) return false;
							if(Linked==Direction.Forward) return false;
							Link.LinkTo(null,dir);}
					break;default:return false;}
				Link=what;Linked=where;
				what.LinkTo(this,dir);return true;}
			_=(what is null)?_Unlink(where):_Link(what,where);}
	public bool Run(){
			if(Pending&&Linked==Direction.Forward)Link?.Load(Active);
			Pending=false;return true;}}}