using System;
using System.Collections.Generic;
using System.Linq;

namespace ECAD {
	internal class Collection<T>:Named,Clocked where T:Clocked{
		public List<T> Items{get;set;}
		public bool Empty{get => Items.Count==0;}
		public T this[int index]{//<--this should catch index out of range exception... can this return null?
			get => Items[index];
			set => Items[index]=value;}
		public int Count{get => Items.Count;}
		public Collection(string label=""):base(label) => Items=new();
		public T? Get(string? s) => (string.IsNullOrEmpty(s))?default:Items.FirstOrDefault(i => (i as Named)?.Name==s);
		public bool InRange(int i) => i>=0&&i<Items.Count;
		public void ForEach(Action<T> action) => Items.ForEach(action);
		public int CountWhere(Func<T,bool> condition) => Items.Where(condition).Count();
		public List<T> Where(Func<T,bool> condition) => Items.Where(condition).ToList();
		public T? FirstOrDefault(Func<T,bool> condition) => Items.Where(condition).FirstOrDefault();
		public T First(Func<T,bool> condition) => Items.Where(condition).First();
		public bool Any(Func<T,bool> condition) => Items.Any(condition);
		public bool Run(){bool run=false;
			Items.ForEach(i => run=i.Run()||run);
			return run;}}}