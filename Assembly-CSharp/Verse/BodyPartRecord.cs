using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Verse
{
	public class BodyPartRecord
	{
		public BodyPartDef def;

		public List<BodyPartRecord> parts = new List<BodyPartRecord>();

		public BodyPartHeight height;

		public BodyPartDepth depth;

		public float coverage = 1f;

		public List<BodyPartGroupDef> groups = new List<BodyPartGroupDef>();

		[Unsaved]
		public BodyPartRecord parent;

		[Unsaved]
		public float coverageAbsWithChildren;

		[Unsaved]
		public float coverageAbs;

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"BodyPartRecord(",
				(this.def == null) ? "NULL_DEF" : this.def.defName,
				" parts.Count=",
				this.parts.Count,
				")"
			});
		}

		public bool IsInGroup(BodyPartGroupDef group)
		{
			for (int i = 0; i < this.groups.Count; i++)
			{
				if (this.groups[i] == group)
				{
					return true;
				}
			}
			return false;
		}

		[DebuggerHidden]
		public IEnumerable<BodyPartRecord> GetChildParts(string tag)
		{
			BodyPartRecord.<GetChildParts>c__Iterator1BE <GetChildParts>c__Iterator1BE = new BodyPartRecord.<GetChildParts>c__Iterator1BE();
			<GetChildParts>c__Iterator1BE.tag = tag;
			<GetChildParts>c__Iterator1BE.<$>tag = tag;
			<GetChildParts>c__Iterator1BE.<>f__this = this;
			BodyPartRecord.<GetChildParts>c__Iterator1BE expr_1C = <GetChildParts>c__Iterator1BE;
			expr_1C.$PC = -2;
			return expr_1C;
		}

		public bool HasChildParts(string tag)
		{
			return this.GetChildParts(tag).Any<BodyPartRecord>();
		}

		[DebuggerHidden]
		public IEnumerable<BodyPartRecord> GetConnectedParts(string tag)
		{
			BodyPartRecord.<GetConnectedParts>c__Iterator1BF <GetConnectedParts>c__Iterator1BF = new BodyPartRecord.<GetConnectedParts>c__Iterator1BF();
			<GetConnectedParts>c__Iterator1BF.tag = tag;
			<GetConnectedParts>c__Iterator1BF.<$>tag = tag;
			<GetConnectedParts>c__Iterator1BF.<>f__this = this;
			BodyPartRecord.<GetConnectedParts>c__Iterator1BF expr_1C = <GetConnectedParts>c__Iterator1BF;
			expr_1C.$PC = -2;
			return expr_1C;
		}
	}
}
