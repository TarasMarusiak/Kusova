using System;

namespace BureauApp.Models
{
    public abstract class Item
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string Place { get; set; } = "";
        public string Category { get; set; } = "Інше";
        public bool IsArchived { get; set; } = false;
        public string ImagePath { get; set; } = "";

        public abstract string GetSummary();
    }

    public class FoundItem : Item 
    { 
        public override string GetSummary() => $"Знайдено в: {Place}"; 
    }

    public class LostItem : Item 
    { 
        public decimal Reward { get; set; } 
        public override string GetSummary() => $"Винагорода: {Reward} грн"; 
    }
}