using System;
using System.Linq;

namespace ECAD {
	internal abstract class Component:Named,Clocked{
		public Collection<Port> Pins{get;private set;}
		public string Type{get;private set;}
		public Component? Parrent {get;private set;}
		public bool Pending{get => Pins.Where(p => p.Type==Behaviour.Input).Any(ioPin => ioPin.Pending);}
		public Component(string name,string type,Component? parrent=null):base(name){Pins=new("Ports");Type=type;Parrent=parrent; }
		public abstract bool Run();
		//display utils
		public int xSize() => 3+Math.Max(
			Pins.CountWhere(pin => pin.side==Direction.North),
			Pins.CountWhere(pin => pin.side==Direction.South));
		public int ySize() => 3+Math.Max(
			Pins.CountWhere(pin => pin.side==Direction.West),
			Pins.CountWhere(pin => pin.side==Direction.East));
		public void Rotate(){
			Pins.Where(pin => pin.side==Direction.West).ForEach(ioPin => ioPin.side=Direction.Unset);
			Pins.Where(pin => pin.side==Direction.South).ForEach(ioPin => ioPin.side=Direction.West);
			Pins.Where(pin => pin.side==Direction.East).ForEach(ioPin => ioPin.side=Direction.South);
			Pins.Where(pin => pin.side==Direction.North).ForEach(ioPin => ioPin.side=Direction.East);
			Pins.Where(pin => pin.side==Direction.Unset).ForEach(ioPin => ioPin.side=Direction.North);}
		public int hFlip(){
			Pins.Where(pin => pin.side==Direction.North).ForEach(ioPin => ioPin.side=Direction.Unset);
			Pins.Where(pin => pin.side==Direction.South).ForEach(ioPin => ioPin.side=Direction.North);
			Pins.Where(pin => pin.side==Direction.Unset).ForEach(ioPin => ioPin.side=Direction.South);
			return 0;}
		public int vFlip(){
			Pins.Where(pin => pin.side==Direction.West).ForEach(ioPin => ioPin.side=Direction.Unset);
			Pins.Where(pin => pin.side==Direction.East).ForEach(ioPin => ioPin.side=Direction.West);
			Pins.Where(pin => pin.side==Direction.Unset).ForEach(ioPin => ioPin.side=Direction.East);
			return 0;}}}