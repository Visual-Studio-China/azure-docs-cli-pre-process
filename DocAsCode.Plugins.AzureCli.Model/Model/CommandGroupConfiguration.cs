using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    public class CommandGroupConfiguration
    {
        public string CommandPrefix { get; set; }

        public string Language { get; set; }
    }

    public enum CommandGroupType
    {
        AZURE,

        VSTS
    }
}
