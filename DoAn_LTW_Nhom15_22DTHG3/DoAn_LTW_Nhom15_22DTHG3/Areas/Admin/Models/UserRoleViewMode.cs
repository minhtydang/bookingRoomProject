using DoAn_LTW_Nhom15_22DTHG3.Models;
using System.Collections.Generic;

namespace DoAn_LTW_Nhom15_22DTHG3.Areas.Admin.ViewModels
{
    public class UserRoleViewModel
    {
        public ApplicationUser User { get; set; }
        public IList<string> Roles { get; set; }
    }
}