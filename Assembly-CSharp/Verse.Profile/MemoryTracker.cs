using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Verse.Profile
{
	public static class MemoryTracker
	{
		private class ReferenceData
		{
			public struct Link
			{
				public object target;

				public MemoryTracker.ReferenceData targetRef;

				public string path;
			}

			public List<MemoryTracker.ReferenceData.Link> refers = new List<MemoryTracker.ReferenceData.Link>();

			public List<MemoryTracker.ReferenceData.Link> referredBy = new List<MemoryTracker.ReferenceData.Link>();

			public string path;

			public int pathCost;
		}

		private struct ChildReference
		{
			public object child;

			public string path;
		}

		public class MarkupComplete : Attribute
		{
		}

		private const int updatesPerCull = 10;

		private static Dictionary<Type, HashSet<WeakReference>> tracked = new Dictionary<Type, HashSet<WeakReference>>();

		private static bool trackedLocked = false;

		private static List<object> trackedQueue = new List<object>();

		private static List<RuntimeTypeHandle> trackedTypeQueue = new List<RuntimeTypeHandle>();

		private static int updatesSinceLastCull = 0;

		private static int cullTargetIndex = 0;

		public static void RegisterObject(object obj)
		{
			if (MemoryTracker.trackedLocked)
			{
				MemoryTracker.trackedQueue.Add(obj);
				return;
			}
			Type type = obj.GetType();
			HashSet<WeakReference> hashSet = null;
			if (!MemoryTracker.tracked.TryGetValue(type, out hashSet))
			{
				hashSet = new HashSet<WeakReference>();
				MemoryTracker.tracked[type] = hashSet;
			}
			hashSet.Add(new WeakReference(obj));
		}

		public static void RegisterType(RuntimeTypeHandle typeHandle)
		{
			if (MemoryTracker.trackedLocked)
			{
				MemoryTracker.trackedTypeQueue.Add(typeHandle);
				return;
			}
			Type typeFromHandle = Type.GetTypeFromHandle(typeHandle);
			if (!MemoryTracker.tracked.ContainsKey(typeFromHandle))
			{
				MemoryTracker.tracked[typeFromHandle] = new HashSet<WeakReference>();
			}
		}

		private static void LockTracking()
		{
			if (MemoryTracker.trackedLocked)
			{
				throw new NotImplementedException();
			}
			MemoryTracker.trackedLocked = true;
		}

		private static void UnlockTracking()
		{
			if (!MemoryTracker.trackedLocked)
			{
				throw new NotImplementedException();
			}
			MemoryTracker.trackedLocked = false;
			foreach (object current in MemoryTracker.trackedQueue)
			{
				MemoryTracker.RegisterObject(current);
			}
			MemoryTracker.trackedQueue.Clear();
			foreach (RuntimeTypeHandle current2 in MemoryTracker.trackedTypeQueue)
			{
				MemoryTracker.RegisterType(current2);
			}
			MemoryTracker.trackedTypeQueue.Clear();
		}

		public static void LogObjectsLoaded()
		{
			if (MemoryTracker.tracked.Count == 0)
			{
				Log.Message("No objects tracked, memory tracker markup may not be applied.");
				return;
			}
			GC.Collect();
			MemoryTracker.LockTracking();
			try
			{
				foreach (HashSet<WeakReference> current in MemoryTracker.tracked.Values)
				{
					MemoryTracker.CullNulls(current);
				}
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<Type, HashSet<WeakReference>> current2 in from kvp in MemoryTracker.tracked
				orderby -kvp.Value.Count
				select kvp)
				{
					stringBuilder.AppendLine(string.Format("{0,6} {1}", current2.Value.Count, current2.Key));
				}
				Log.Message(stringBuilder.ToString());
			}
			finally
			{
				MemoryTracker.UnlockTracking();
			}
		}

		public static void LogObjectHoldPaths()
		{
			if (MemoryTracker.tracked.Count == 0)
			{
				Log.Message("No objects tracked, memory tracker markup may not be applied.");
				return;
			}
			GC.Collect();
			MemoryTracker.LockTracking();
			try
			{
				foreach (HashSet<WeakReference> current in MemoryTracker.tracked.Values)
				{
					MemoryTracker.CullNulls(current);
				}
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				foreach (KeyValuePair<Type, HashSet<WeakReference>> current2 in from kvp in MemoryTracker.tracked
				orderby -kvp.Value.Count
				select kvp)
				{
					KeyValuePair<Type, HashSet<WeakReference>> elementLocal = current2;
					list.Add(new FloatMenuOption(string.Format("{0} ({1})", current2.Key, current2.Value.Count), delegate
					{
						MemoryTracker.LogObjectHoldPathsFor(elementLocal.Key, elementLocal.Value);
					}, MenuOptionPriority.Default, null, null, 0f, null, null));
					if (list.Count == 30)
					{
						break;
					}
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
			finally
			{
				MemoryTracker.UnlockTracking();
			}
		}

		public static void LogObjectHoldPathsFor(Type type, HashSet<WeakReference> elements)
		{
			GC.Collect();
			MemoryTracker.LockTracking();
			try
			{
				Dictionary<object, MemoryTracker.ReferenceData> dictionary = new Dictionary<object, MemoryTracker.ReferenceData>();
				HashSet<object> hashSet = new HashSet<object>();
				Queue<object> queue = new Queue<object>();
				foreach (object current in from weakref in MemoryTracker.tracked.SelectMany((KeyValuePair<Type, HashSet<WeakReference>> kvp) => kvp.Value)
				where weakref.IsAlive
				select weakref.Target)
				{
					if (!hashSet.Contains(current))
					{
						hashSet.Add(current);
						queue.Enqueue(current);
					}
				}
				foreach (Type current2 in GenTypes.AllTypes.Union(MemoryTracker.tracked.Keys))
				{
					if (!current2.FullName.Contains("MemoryTracker"))
					{
						if (!current2.ContainsGenericParameters)
						{
							MemoryTracker.AccumulateStaticMembers(current2, dictionary, hashSet, queue);
						}
					}
				}
				int num = 0;
				while (queue.Count > 0)
				{
					if (num % 10000 == 0)
					{
						UnityEngine.Debug.LogFormat("{0} / {1} (to process: {2})", new object[]
						{
							num,
							num + queue.Count,
							queue.Count
						});
					}
					num++;
					MemoryTracker.AccumulateReferences(queue.Dequeue(), dictionary, hashSet, queue);
				}
				int num2 = 0;
				MemoryTracker.CalculateReferencePaths(dictionary, from kvp in dictionary
				where !kvp.Value.path.NullOrEmpty()
				select kvp.Key, num2);
				num2 += 1000;
				MemoryTracker.CalculateReferencePaths(dictionary, from kvp in dictionary
				where kvp.Value.path.NullOrEmpty() && kvp.Value.referredBy.Count == 0
				select kvp.Key, num2);
				foreach (object current3 in from kvp in dictionary
				where kvp.Value.path.NullOrEmpty()
				select kvp.Key)
				{
					num2 += 1000;
					MemoryTracker.CalculateReferencePaths(dictionary, new object[]
					{
						current3
					}, num2);
				}
				Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
				foreach (WeakReference current4 in elements)
				{
					if (current4.IsAlive)
					{
						string path = dictionary[current4.Target].path;
						if (dictionary2.ContainsKey(path))
						{
							Dictionary<string, int> dictionary3;
							Dictionary<string, int> expr_34D = dictionary3 = dictionary2;
							string key;
							string expr_352 = key = path;
							int num3 = dictionary3[key];
							expr_34D[expr_352] = num3 + 1;
						}
						else
						{
							dictionary2[path] = 1;
						}
					}
				}
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<string, int> current5 in from kvp in dictionary2
				orderby -kvp.Value
				select kvp)
				{
					stringBuilder.AppendLine(string.Format("{0}: {1}", current5.Value, current5.Key));
				}
				Log.Message(stringBuilder.ToString());
			}
			finally
			{
				MemoryTracker.UnlockTracking();
			}
		}

		private static void AccumulateReferences(object obj, Dictionary<object, MemoryTracker.ReferenceData> references, HashSet<object> seen, Queue<object> toProcess)
		{
			MemoryTracker.ReferenceData referenceData = null;
			if (!references.TryGetValue(obj, out referenceData))
			{
				referenceData = new MemoryTracker.ReferenceData();
				references[obj] = referenceData;
			}
			foreach (MemoryTracker.ChildReference current in MemoryTracker.GetAllReferencedClassesFromClassOrStruct(obj, MemoryTracker.GetFieldsFromHierarchy(obj.GetType(), BindingFlags.Instance), obj, string.Empty))
			{
				if (!current.child.GetType().IsClass)
				{
					throw new ApplicationException();
				}
				MemoryTracker.ReferenceData referenceData2 = null;
				if (!references.TryGetValue(current.child, out referenceData2))
				{
					referenceData2 = new MemoryTracker.ReferenceData();
					references[current.child] = referenceData2;
				}
				referenceData2.referredBy.Add(new MemoryTracker.ReferenceData.Link
				{
					target = obj,
					targetRef = referenceData,
					path = current.path
				});
				referenceData.refers.Add(new MemoryTracker.ReferenceData.Link
				{
					target = current.child,
					targetRef = referenceData2,
					path = current.path
				});
				if (!seen.Contains(current.child))
				{
					seen.Add(current.child);
					toProcess.Enqueue(current.child);
				}
			}
		}

		private static void AccumulateStaticMembers(Type type, Dictionary<object, MemoryTracker.ReferenceData> references, HashSet<object> seen, Queue<object> toProcess)
		{
			foreach (MemoryTracker.ChildReference current in MemoryTracker.GetAllReferencedClassesFromClassOrStruct(null, MemoryTracker.GetFields(type, BindingFlags.Static), null, type.ToString() + "."))
			{
				if (!current.child.GetType().IsClass)
				{
					throw new ApplicationException();
				}
				MemoryTracker.ReferenceData referenceData = null;
				if (!references.TryGetValue(current.child, out referenceData))
				{
					referenceData = new MemoryTracker.ReferenceData();
					referenceData.path = current.path;
					referenceData.pathCost = 0;
					references[current.child] = referenceData;
				}
				if (!seen.Contains(current.child))
				{
					seen.Add(current.child);
					toProcess.Enqueue(current.child);
				}
			}
		}

		[DebuggerHidden]
		private static IEnumerable<MemoryTracker.ChildReference> GetAllReferencedClassesFromClassOrStruct(object current, IEnumerable<FieldInfo> fields, object parent, string currentPath)
		{
			MemoryTracker.<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C <GetAllReferencedClassesFromClassOrStruct>c__Iterator21C = new MemoryTracker.<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C();
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.fields = fields;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.current = current;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.parent = parent;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.currentPath = currentPath;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.<$>fields = fields;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.<$>current = current;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.<$>parent = parent;
			<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C.<$>currentPath = currentPath;
			MemoryTracker.<GetAllReferencedClassesFromClassOrStruct>c__Iterator21C expr_3F = <GetAllReferencedClassesFromClassOrStruct>c__Iterator21C;
			expr_3F.$PC = -2;
			return expr_3F;
		}

		[DebuggerHidden]
		private static IEnumerable<MemoryTracker.ChildReference> DistillChildReferencesFromObject(object current, object parent, string currentPath)
		{
			MemoryTracker.<DistillChildReferencesFromObject>c__Iterator21D <DistillChildReferencesFromObject>c__Iterator21D = new MemoryTracker.<DistillChildReferencesFromObject>c__Iterator21D();
			<DistillChildReferencesFromObject>c__Iterator21D.current = current;
			<DistillChildReferencesFromObject>c__Iterator21D.currentPath = currentPath;
			<DistillChildReferencesFromObject>c__Iterator21D.parent = parent;
			<DistillChildReferencesFromObject>c__Iterator21D.<$>current = current;
			<DistillChildReferencesFromObject>c__Iterator21D.<$>currentPath = currentPath;
			<DistillChildReferencesFromObject>c__Iterator21D.<$>parent = parent;
			MemoryTracker.<DistillChildReferencesFromObject>c__Iterator21D expr_31 = <DistillChildReferencesFromObject>c__Iterator21D;
			expr_31.$PC = -2;
			return expr_31;
		}

		[DebuggerHidden]
		private static IEnumerable<FieldInfo> GetFieldsFromHierarchy(Type type, BindingFlags bindingFlags)
		{
			MemoryTracker.<GetFieldsFromHierarchy>c__Iterator21E <GetFieldsFromHierarchy>c__Iterator21E = new MemoryTracker.<GetFieldsFromHierarchy>c__Iterator21E();
			<GetFieldsFromHierarchy>c__Iterator21E.type = type;
			<GetFieldsFromHierarchy>c__Iterator21E.bindingFlags = bindingFlags;
			<GetFieldsFromHierarchy>c__Iterator21E.<$>type = type;
			<GetFieldsFromHierarchy>c__Iterator21E.<$>bindingFlags = bindingFlags;
			MemoryTracker.<GetFieldsFromHierarchy>c__Iterator21E expr_23 = <GetFieldsFromHierarchy>c__Iterator21E;
			expr_23.$PC = -2;
			return expr_23;
		}

		[DebuggerHidden]
		private static IEnumerable<FieldInfo> GetFields(Type type, BindingFlags bindingFlags)
		{
			MemoryTracker.<GetFields>c__Iterator21F <GetFields>c__Iterator21F = new MemoryTracker.<GetFields>c__Iterator21F();
			<GetFields>c__Iterator21F.type = type;
			<GetFields>c__Iterator21F.bindingFlags = bindingFlags;
			<GetFields>c__Iterator21F.<$>type = type;
			<GetFields>c__Iterator21F.<$>bindingFlags = bindingFlags;
			MemoryTracker.<GetFields>c__Iterator21F expr_23 = <GetFields>c__Iterator21F;
			expr_23.$PC = -2;
			return expr_23;
		}

		private static void CalculateReferencePaths(Dictionary<object, MemoryTracker.ReferenceData> references, IEnumerable<object> objects, int pathCost)
		{
			Queue<object> queue = new Queue<object>(objects);
			while (queue.Count > 0)
			{
				object obj = queue.Dequeue();
				if (references[obj].path.NullOrEmpty())
				{
					references[obj].path = string.Format("???.{0}", obj.GetType());
					references[obj].pathCost = pathCost;
				}
				MemoryTracker.CalculateObjectReferencePath(obj, references, queue);
			}
		}

		private static void CalculateObjectReferencePath(object obj, Dictionary<object, MemoryTracker.ReferenceData> references, Queue<object> queue)
		{
			MemoryTracker.ReferenceData referenceData = references[obj];
			foreach (MemoryTracker.ReferenceData.Link current in referenceData.refers)
			{
				MemoryTracker.ReferenceData referenceData2 = references[current.target];
				string text = referenceData.path + "." + current.path;
				int num = referenceData.pathCost + 1;
				if (referenceData2.path.NullOrEmpty())
				{
					queue.Enqueue(current.target);
					referenceData2.path = text;
					referenceData2.pathCost = num;
				}
				else if (referenceData2.pathCost == num && referenceData2.path.CompareTo(text) < 0)
				{
					referenceData2.path = text;
				}
				else if (referenceData2.pathCost > num)
				{
					throw new ApplicationException();
				}
			}
		}

		public static void Update()
		{
			if (MemoryTracker.tracked.Count == 0)
			{
				return;
			}
			if (MemoryTracker.updatesSinceLastCull++ >= 10)
			{
				MemoryTracker.updatesSinceLastCull = 0;
				KeyValuePair<Type, HashSet<WeakReference>> keyValuePair = MemoryTracker.tracked.ElementAtOrDefault(MemoryTracker.cullTargetIndex++);
				if (keyValuePair.Value == null)
				{
					MemoryTracker.cullTargetIndex = 0;
				}
				else
				{
					MemoryTracker.CullNulls(keyValuePair.Value);
				}
			}
		}

		private static void CullNulls(HashSet<WeakReference> table)
		{
			table.RemoveWhere((WeakReference element) => !element.IsAlive);
		}
	}
}
