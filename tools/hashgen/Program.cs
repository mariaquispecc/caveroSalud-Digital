using Microsoft.AspNetCore.Identity;
using System;

var hasher = new PasswordHasher<IdentityUser<Guid>>();
var user = new IdentityUser<Guid>();
var pwd = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "P@ssw0rd!";
var hash = hasher.HashPassword(user, pwd);
Console.WriteLine(hash);
