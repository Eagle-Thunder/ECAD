namespace ECAD {
	internal abstract class Named{
		public string Name{get;set;}
		public double xPos{get;set;}//req 4 display
		public double yPos{get;set;}//req 4 display
		public Named(string s=""){Name=s;xPos=yPos=0;}
		public override string ToString() => Name;}}