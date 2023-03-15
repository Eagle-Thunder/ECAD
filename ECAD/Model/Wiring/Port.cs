using System.Linq;

namespace ECAD {
	internal class Port:Collection<Pin>{
		public Component Parrent{get;private set;}
		public Behaviour Type{get;private set;}
		public Direction side{get;set;}//req 4 display
		public bool Active{get => Items.Any(pin => pin.Active);}
		public bool Pending{get => Items.Any(pin => pin.Pending);}
		public bool Linked{get => Items.Any(pin => pin.Linked!=Direction.Unset);}
		public bool LinkedFw{get => Items.Any(pin => pin.Linked==Direction.Forward);}
		public Port(string label,Component p,int count=1,Direction d=Direction.Unset,Behaviour t=Behaviour.Unset):base(label){
			side=d;Type=t;Parrent=p;
			for(int i=0;i<count;i++)Items.Add(new(this,i));}
		public bool LinkTo(Port? target){
			if(target is not null&&Count!=target.Count) return false;//<--this should probably be void and throw an exception
			for(int i=0;InRange(i);i++)this[i].LinkTo(target?.Items[i],Direction.Forward);
			return true;}}}