﻿using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"${ProjectName}", 
	Namespace = "${ProjectName}",
	Version = "1.0"
)]

[assembly:AddinName ("${ProjectName}")]
[assembly:AddinCategory ("IDE extensions")]
[assembly:AddinDescription ("${ProjectName}")]
[assembly:AddinAuthor ("${AuthorName}")]
