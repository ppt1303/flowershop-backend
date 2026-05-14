using System;
using System.Collections.Generic;

namespace FlowerShop.Data;

public partial class Contact
{
    public int ContactId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedDate { get; set; }
}
