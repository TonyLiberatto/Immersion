using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;

namespace Neolithic
{
    class BehaviorSlaughterable : EntityBehavior
    {
        public BehaviorSlaughterable(Entity entity) : base(entity) { }
        public override string PropertyName() => "slaughterable";
    }
}
