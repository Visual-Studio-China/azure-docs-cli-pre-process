using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    public class TocTitleMappings
    {
        public string TocTitle { get; set; }

        public string PageTitle { get; set; }

        public bool Show { get; set; } = true;
    }
}
