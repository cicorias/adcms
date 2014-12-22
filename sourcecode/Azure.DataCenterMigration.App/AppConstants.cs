/*******************************************************************************
 * Copyright 2014 Persistent Systems Ltd.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 ******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.DataCenterMigration.App
{
    /// <summary>
    /// This class contains the variables to store the constant values for Azure.DataCenterMigration.App.
    /// </summary>
    internal class AppConstants
    {
        /// <summary>
        /// Constant to store 'command line arguments' string.
        /// </summary>
        internal const string CommandLineArguments = "command line arguments";

        /// <summary>
        /// Constant to store '-{0}' string.
        /// </summary>
        internal const string CommandLineParam = "-{0}";
        
        /// <summary>
        /// Constant to store '-Help' string.
        /// </summary>
        internal const string Help = "-Help";

        /// <summary>
        /// Constant to store 'y' string.
        /// </summary>
        internal const char Yes = 'y';

        /// <summary>
        /// Constant to store 'n' string.
        /// </summary>
        internal const char No = 'n';

    }
}
