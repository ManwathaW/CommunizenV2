using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CommuniZEN.ViewModels
{
    public partial class DailyAffirmationsViewModel : ObservableObject
    {
         
        private readonly INavigation _navigation;
        private readonly Random _random = new Random();

        [ObservableProperty]
        private string currentAffirmation;

        [ObservableProperty]
        private string currentBackground;

        private readonly string[] _affirmations = new[]
        {
              // Self-Worth & Confidence
    "I am capable of achieving great things",
    "I choose to be confident and self-assured",
    "I am worthy of love, respect, and happiness",
    "I trust in my ability to create positive change",
    "Every day I grow stronger and wiser",
    "I embrace the beauty of this moment",
    "My potential to succeed is infinite",
    "I radiate positivity and attract goodness",
    "I am in charge of my own happiness",
    "My possibilities are endless",
    
    // Personal Growth
    "I am becoming the best version of myself",
    "I learn and grow from every experience",
    "I face challenges with courage and wisdom",
    "Every day brings new opportunities for growth",
    "I am constantly evolving and improving",
    "My strength grows with every challenge I face",
    "I embrace change as a path to growth",
    "I am open to learning new things",
    
    // Inner Peace
    "I choose peace over worry",
    "I am at peace with where I am right now",
    "My mind is calm and my heart is at peace",
    "I release all tension and embrace tranquility",
    "I am centered, peaceful, and grounded",
    "Tranquility flows through me with every breath",
    
    // Resilience
    "I can overcome any obstacle",
    "I am resilient and bounce back stronger",
    "My challenges are opportunities in disguise",
    "I possess the strength to handle anything",
    "Every setback is a setup for a comeback",
    "I turn obstacles into stepping stones",
    
    // Success & Achievement
    "I attract success naturally",
    "I am worthy of all my achievements",
    "Success flows to me effortlessly",
    "I create my own opportunities",
    "I deserve abundance and prosperity",
    "My actions lead to successful outcomes",
    
    // Self-Love
    "I love and accept myself completely",
    "I am enough, just as I am",
    "I deserve all the good things life offers",
    "I treat myself with kindness and respect",
    "I honor my needs and feelings",
    "My self-worth is not determined by others",
    
    // Mindfulness
    "I am present in this moment",
    "Each breath brings me peace and clarity",
    "I am grounded in the present moment",
    "I choose to focus on what truly matters",
    "My mind is clear and my thoughts are positive",
    
    // Relationships
    "I attract positive and uplifting relationships",
    "I am surrounded by love and support",
    "I deserve healthy and nurturing relationships",
    "I am a magnet for genuine connections",
    "My relationships are growing stronger each day",
    
    // Purpose & Direction
    "I am on the right path",
    "My life has meaning and purpose",
    "I trust in my journey",
    "Every day I move closer to my goals",
    "I am aligned with my true purpose",
    
    // Gratitude
    "I am grateful for all that I have",
    "My life is filled with blessings",
    "I find joy in the simple moments",
    "I appreciate the beauty around me",
    "Each day brings new things to be thankful for",
    
    // Health & Vitality
    "My body is healthy and full of energy",
    "I take care of myself with love",
    "I am becoming stronger and healthier each day",
    "My mind and body are in perfect harmony",
    "I radiate health and vitality"
        };

        private readonly string[] _backgrounds = new[]
        {
            "bg1.jpg",
            "bg2.jpg",
            "bg3.jpg",
            "bg4.jpg",
            "bg5.jpg",
            "bg6.jpg",
            "bg7.jpg",
            "gb8.jpg",
            "gb9.jpg"
        };

        public DailyAffirmationsViewModel(INavigation navigation)
        {
            _navigation = navigation;
            GetRandomContent();
        }

        [RelayCommand]
        private void GetRandomContent()
        {
            // Get random affirmation
            var affirmationIndex = _random.Next(_affirmations.Length);
            CurrentAffirmation = _affirmations[affirmationIndex];

            // Get random background
            var backgroundIndex = _random.Next(_backgrounds.Length);
            CurrentBackground = _backgrounds[backgroundIndex];
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await _navigation.PopAsync();
        }
    }

}
