﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentProject.Models;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly StudentDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(StudentDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest model)
    {
        var user = _context.UserModel.SingleOrDefault(u => u.Username == model.Username);

        if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
        {
            return Unauthorized(); // Invalid username or password
        }

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(UserModel user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // Implement your password verification logic here (e.g., using bcrypt)
        // For simplicity, this example does not include actual password hashing.
        // You should use a secure hashing algorithm in your production code.
        return password == passwordHash;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest model)
    {
        // Check if a user with the same username already exists
        if (_context.UserModel.Any(u => u.Username == model.Username))
        {
            return BadRequest("Username already exists");
        }

        // You should hash the password before storing it in the database
        // For simplicity, this example does not include actual password hashing.
        // You should use a secure hashing algorithm in your production code.

        var user = new UserModel
        {
            Username = model.Username,
            PasswordHash = model.Password // In a real application, hash the password
        };

        _context.UserModel.Add(user);
        _context.SaveChanges();

        // Generate and return a JWT token for the newly registered user
        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

}
