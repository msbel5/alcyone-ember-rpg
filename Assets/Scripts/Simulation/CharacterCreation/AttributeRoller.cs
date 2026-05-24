using System;
using System.Collections.ObjectModel;

namespace EmberCrpg.Simulation.CharacterCreation
{
    public sealed class AttributeRoll
    {
        public AttributeRoll(string attribute, int[] dice, int droppedIndex)
        {
            Attribute = attribute ?? string.Empty;
            Dice = new ReadOnlyCollection<int>((int[])dice.Clone());
            DroppedIndex = droppedIndex;
            DroppedValue = dice[droppedIndex];
            Total = 0;
            for (int i = 0; i < dice.Length; i++) if (i != droppedIndex) Total += dice[i];
        }

        public string Attribute { get; }
        public ReadOnlyCollection<int> Dice { get; }
        public int DroppedIndex { get; }
        public int DroppedValue { get; }
        public int Total { get; }
        public string LogLine => "[roll] " + Attribute + " = " + Dice[0] + "+" + Dice[1] + "+" + Dice[2] + "+(" + Dice[3] + ") = " + Total + ".";
    }

    public static class AttributeRoller
    {
        public static AttributeRoll Roll4d6DropLowest(uint seed, string attribute)
        {
            var dice = new int[4];
            for (int i = 0; i < dice.Length; i++) dice[i] = RollDie(seed, attribute ?? string.Empty, i);
            int dropped = 0;
            for (int i = 1; i < dice.Length; i++) if (dice[i] < dice[dropped]) dropped = i;
            return new AttributeRoll(attribute, dice, dropped);
        }

        private static int RollDie(uint seed, string attribute, int index)
        {
            unchecked
            {
                uint value = seed + 0x9E3779B9u * (uint)(index + 1);
                for (int i = 0; i < attribute.Length; i++) value = (value ^ attribute[i]) * 16777619u;
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                return (int)(value % 6u) + 1;
            }
        }
    }
}
