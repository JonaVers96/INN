using System;
using System.Text.Json.Serialization;
using LYL.Domain.Model.Interfaces;
namespace LYL.Domain.Model;

public class Character
{
    [JsonPropertyName("profile")] public CharacterProfile Profile { get; set; }
    public int AllocatablePoints { get; private set; } = 3;
    [JsonPropertyName("studentPhase")] public CharacterStudentPhase StudentPhase { get; set; }
    [JsonPropertyName("career")] public CharacterCareer Career { get; set; }

    public Job? ChosenJob { get; set; }
    
    public LivingSituation LivingSituation {get; set;}

    public void AssignRandomJob(IRandomIntProvider random)
    {
        if (Career.AvailableJobs.Count == 0)
            throw new InvalidOperationException("No jobs available to assign.");

        int randomNumber = random.NextInt(0, Career.AvailableJobs.Count);
        ChosenJob = Career.AvailableJobs[randomNumber];
    }
}
