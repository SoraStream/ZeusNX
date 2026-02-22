using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZeusNX.YYOptions
{
    internal class YYOptionsLTS
    {
        public string resourceType { get; } = "GMSwitchOptions";
        public string resourceVersion { get; } = "1.0";
        public string name { get; } = "Switch";
        public string option_switch_project_nmeta { get; set; }
        public bool option_switch_enable_nex_libraries = false;
        public bool option_switch_interpolate_pixels { get; set; }
        public int option_switch_scale { get; set; }
        public string option_switch_texture_page { get; set; }
        public bool option_switch_check_nsp_publish_errors { get; } = false;
        public bool option_switch_enable_fileaccess_checking { get; set; }
        public string option_switch_splash_screen { get; set; }
        public bool option_switch_use_splash { get; set; }
        public bool option_switch_allow_debug_output { get; set; }

    }

    internal class YYOptions2024
    {
        [JsonProperty("$GMSwitchOptions")]
        public string GMSwitchOptions { get; } = "";

        [JsonProperty("%Name")]
        public string Name { get; } = "Switch";
        public string name { get; } = "Switch";
        public bool option_switch_allow_debug_output { get; set; }
        public bool option_switch_check_nsp_publish_errors { get; set; }
        public bool option_switch_enable_fileaccess_checking { get; set; }
        public bool option_switch_enable_nex_libraries { get; } = false;
        public bool option_switch_enable_npln_libraries { get; } = false;
        public int option_switch_gfx_mem_mb { get; } = 256;
        public bool option_switch_interpolate_pixels { get; set; }
        public string option_switch_project_nmeta { get; set; }
        public int option_switch_scale { get; set; }
        public string option_switch_splash_screen { get; set; }
        public string option_switch_texture_page { get; set; }
        public bool option_switch_use_splash { get; set; }
        public string resourceType { get; } = "GMSwitchOptions";
        public string resourceVersion { get; } = "2.0";

    }
}
