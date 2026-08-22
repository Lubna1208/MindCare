using Microsoft.EntityFrameworkCore;
using MindCare.Models;

namespace MindCare.Data;

public static class ResourceSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Resources.AnyAsync())
        {
            return;
        }

        var createdAt = DateTime.UtcNow;
        context.Resources.AddRange(
            new Resource
            {
                Title = "General Mental Wellbeing",
                Category = "Articles",
                Description = "A short introduction to everyday habits that can support your wellbeing.",
                Content = "Wellbeing can be supported through small, regular actions. Consider keeping a routine, staying connected with people you trust, making time for rest, and noticing what helps you feel grounded.",
                CreatedAt = createdAt
            },
            new Resource
            {
                Title = "Managing Everyday Stress",
                Category = "Stress Management",
                Description = "Simple techniques for managing everyday academic and personal stress.",
                Content = "Try breaking large tasks into smaller steps, planning a short pause between activities, and focusing on one next action. Slow breathing, a brief walk, or speaking with someone you trust can also help during a busy day.",
                CreatedAt = createdAt
            },
            new Resource
            {
                Title = "Healthy Sleep Habits",
                Category = "Sleep",
                Description = "Practical ideas for creating a more consistent and restful sleep routine.",
                Content = "Aim for a regular sleep and wake time when possible. Create a calm wind-down routine, reduce screen use before bed, and make your sleeping space comfortable. If sleep concerns continue, consider speaking with a qualified health professional.",
                CreatedAt = createdAt
            },
            new Resource
            {
                Title = "Understanding Anxiety",
                Category = "Anxiety",
                Description = "A general overview of anxious feelings and supportive ways to respond.",
                Content = "Anxious feelings can show up during uncertain or demanding times. Naming the feeling, slowing your breathing, and focusing on the present moment may help. Support from a trusted person or qualified professional can be useful when these feelings are difficult to manage.",
                CreatedAt = createdAt
            },
            new Resource
            {
                Title = "Building Emotional Resilience",
                Category = "Emotional Wellbeing",
                Description = "Ways to build supportive routines and respond kindly to difficult emotions.",
                Content = "Resilience does not mean avoiding difficult emotions. It can include recognising what you feel, using your support network, and making time for activities that restore your energy. Small, realistic goals can help build confidence over time.",
                CreatedAt = createdAt
            },
            new Resource
            {
                Title = "Finding Immediate Support",
                Category = "Crisis Support",
                Description = "Guidance on reaching out for immediate, local support when you feel unsafe or overwhelmed.",
                Content = "If you or someone else may be in immediate danger, contact local emergency services right away. You can also reach out to a trusted person, local crisis service, or qualified healthcare professional for urgent support.",
                CreatedAt = createdAt
            });

        await context.SaveChangesAsync();
    }
}
