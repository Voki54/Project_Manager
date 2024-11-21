﻿using System.ComponentModel.DataAnnotations.Schema;
using Project_Manager.Models.Enums;

namespace Project_Manager.Models
{
    [Table("JoinProjectRequest")]
	public class JoinProjectRequest
	{
		public Project Project { get; set; }
		public int ProjectId { get; set; }
		public string UserId { get; set; }
		public AppUser AppUser { get; set; }
		public JoinProjectRequestStatus Status { get; set; }
	}
}