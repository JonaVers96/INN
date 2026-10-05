using System;
using System.Collections.Generic;
using LYL.Domain.Model.Interfaces;
namespace LYL.Domain.Model;

public class LivingSituation
{
    public int Children {get; set;}
    public Partner Partner {get; set;}
    public List<Partner> AllPartners {get; set;}
    public bool IsShown = false;
    private readonly IRandomIntProvider _random;

    public LivingSituation(List<Partner> partners, IRandomIntProvider random)
    {
        _random = random;
        AllPartners = partners;
        AssignChildren();
        AssignPartner();
    }

    public void AssignPartner()
    {
        Partner = AllPartners[_random.NextInt(0, AllPartners.Count)];
    }
    public void AssignChildren()
    {
        Children = _random.NextInt(0, 4);
    }

}
