using System;
using System.Collections.Generic;

namespace Google.Protobuf.Reflection
{
	public sealed class EnumDescriptor : DescriptorBase
	{
		private readonly EnumDescriptorProto proto;

		private readonly MessageDescriptor containingType;

		private readonly IList<EnumValueDescriptor> values;

		private readonly Type clrType;

		internal EnumDescriptorProto Proto => proto;

		public override string Name => proto.Name;

		public Type ClrType => clrType;

		public MessageDescriptor ContainingType => containingType;

		public IList<EnumValueDescriptor> Values => values;

		public CustomOptions CustomOptions => Proto.Options?.CustomOptions ?? CustomOptions.Empty;

		internal EnumDescriptor(EnumDescriptorProto proto, FileDescriptor file, MessageDescriptor parent, int index, Type clrType)
			: base(file, file.ComputeFullName(parent, proto.Name), index)
		{
			EnumDescriptor parent2 = this;
			this.proto = proto;
			this.clrType = clrType;
			containingType = parent;
			if (proto.Value.Count == 0)
			{
				throw new DescriptorValidationException(this, "Enums must contain at least one value.");
			}
			values = DescriptorUtil.ConvertAndMakeReadOnly(proto.Value, (EnumValueDescriptorProto value, int i) => new EnumValueDescriptor(value, file, parent2, i));
			base.File.DescriptorPool.AddSymbol(this);
		}

		public EnumValueDescriptor FindValueByNumber(int number)
		{
			return base.File.DescriptorPool.FindEnumValueByNumber(this, number);
		}

		public EnumValueDescriptor FindValueByName(string name)
		{
			return base.File.DescriptorPool.FindSymbol<EnumValueDescriptor>(base.FullName + "." + name);
		}
	}
}
