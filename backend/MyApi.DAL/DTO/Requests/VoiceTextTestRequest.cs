using System;

namespace MyApiProject.MyApi.DAL.DTO.Requests;

public class VoiceTextTestRequest
{
    public string Text { get; set; } = "";
    public string? Language { get; set; } = "ar";
    public string? UserId { get; set; }
}