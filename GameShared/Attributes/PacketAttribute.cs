using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Attributes
{

    [AttributeUsage(AttributeTargets.Class)]
    public class PacketAttribute : Attribute
    {
        public int? Id { get; }

        public PacketAttribute()
        {
        }

        public PacketAttribute(int id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RequireAuthAttribute : Attribute
    {
    }
}
