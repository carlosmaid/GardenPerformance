﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

using SEGarden.Logging;
using SEGarden.Logic;
using SEGarden.Math;

namespace GP.Concealment.World.Entities {

    public interface ConcealableEntity : ObservableEntity {

        #region Properties

        bool IsRevealBlocked { get; }
        bool IsInsideAsteroid { get; }

        #endregion

    }

}