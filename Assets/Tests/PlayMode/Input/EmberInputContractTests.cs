using System;
using System.Linq;
using EmberCrpg.Presentation.Ember.Inputs;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.PlayMode.Input
{
    /// <summary>
    /// E7-020 Stage 0 skeleton.
    /// This fixture documents the legacy facade contract and stays ignored until
    /// Stage 3 wires Input System synthetic events (InputTestFixture).
    /// </summary>
    [Ignore("E7-020 Stage 0 baseline skeleton: enable in Stage 3 with Input System device-event assertions.")]
    public sealed class EmberInputContractTests
    {
        private static readonly ContractMember[] ContractMembers =
        {
            new ContractMember("Move", "Vector2", "Input.GetAxisRaw(\"Horizontal\"), Input.GetAxisRaw(\"Vertical\")"),
            new ContractMember("Look", "Vector2", "Input.GetAxisRaw(\"Mouse X\"), Input.GetAxisRaw(\"Mouse Y\")"),
            new ContractMember("LookSmoothed", "Vector2", "Input.GetAxis(\"Mouse X\"), Input.GetAxis(\"Mouse Y\")"),
            new ContractMember("Sprint", "bool", "Input.GetKey(KeyCode.LeftShift)"),
            new ContractMember("JumpDown", "bool", "Input.GetButtonDown(\"Jump\") || Input.GetKeyDown(KeyCode.Space)"),
            new ContractMember("JumpKeyDown", "bool", "Input.GetKeyDown(KeyCode.Space)"),
            new ContractMember("Interact", "bool", "Input.GetKeyDown(KeyCode.E)"),
            new ContractMember("ToggleCursor", "bool", "Input.GetKeyDown(KeyCode.F1)"),
            new ContractMember("RegenWorld", "bool", "Input.GetKeyDown(KeyCode.R)"),
            new ContractMember("ToggleMap", "bool", "Input.GetKeyDown(KeyCode.Tab)"),
            new ContractMember("SaveQuick", "bool", "Input.GetKeyDown(KeyCode.F5)"),
            new ContractMember("LoadQuick", "bool", "Input.GetKeyDown(KeyCode.F9)"),
            new ContractMember("PauseDown", "bool", "Input.GetKeyDown(KeyCode.Escape)"),
            new ContractMember("PauseHeld", "bool", "Input.GetKey(KeyCode.Escape)"),
            new ContractMember("AttackClick", "bool", "Input.GetMouseButtonDown(0)"),
            new ContractMember("SecondaryClick", "bool", "Input.GetMouseButtonDown(1)"),
            new ContractMember("MeleeSwing", "bool", "Input.GetKeyDown(KeyCode.F)"),
            new ContractMember("NumberKeyDown()", "int", "scan Alpha1..Alpha9; first pressed => 1..9 else 0"),
            new ContractMember("NumberKeyDown(int)", "bool", "range-check 1..9 then Input.GetKeyDown(AlphaN)"),
            new ContractMember("FunctionKeyDown()", "int", "scan F1..F12; first pressed => 1..12 else 0"),
            new ContractMember("KeyDown(KeyCode)", "bool", "Input.GetKeyDown(key)"),
            new ContractMember("Key(KeyCode)", "bool", "Input.GetKey(key)"),
            new ContractMember("MouseDown(int)", "bool", "Input.GetMouseButtonDown(button)"),
            new ContractMember("AxisRaw(string)", "float", "Input.GetAxisRaw(axisName)"),
            new ContractMember("Axis(string)", "float", "Input.GetAxis(axisName)")
        };

        [Test]
        public void FacadeContract_DocumentsTwentyFiveMembers()
        {
            Assert.That(ContractMembers.Length, Is.EqualTo(25));
            Assert.That(ContractMembers.Select(m => m.MemberName).Distinct().Count(), Is.EqualTo(25));
        }

        [Test]
        public void Stage0IdleSnapshot_DocumentsLegacyFacadeShape()
        {
            var snapshot = new[]
            {
                EmberInput.Move.ToString(),
                EmberInput.Look.ToString(),
                EmberInput.LookSmoothed.ToString(),
                EmberInput.Sprint.ToString(),
                EmberInput.JumpDown.ToString(),
                EmberInput.JumpKeyDown.ToString(),
                EmberInput.Interact.ToString(),
                EmberInput.ToggleCursor.ToString(),
                EmberInput.RegenWorld.ToString(),
                EmberInput.ToggleMap.ToString(),
                EmberInput.SaveQuick.ToString(),
                EmberInput.LoadQuick.ToString(),
                EmberInput.PauseDown.ToString(),
                EmberInput.PauseHeld.ToString(),
                EmberInput.AttackClick.ToString(),
                EmberInput.SecondaryClick.ToString(),
                EmberInput.MeleeSwing.ToString(),
                EmberInput.NumberKeyDown().ToString(),
                EmberInput.NumberKeyDown(1).ToString(),
                EmberInput.FunctionKeyDown().ToString(),
                EmberInput.KeyDown(KeyCode.C).ToString(),
                EmberInput.Key(KeyCode.C).ToString(),
                EmberInput.MouseDown(0).ToString(),
                EmberInput.AxisRaw("Horizontal").ToString("F4"),
                EmberInput.Axis("Horizontal").ToString("F4")
            };

            Assert.That(snapshot.Length, Is.EqualTo(25));
            Assert.That(ContractMembers.Any(m => m.MemberName == "Move"), Is.True);
        }

        private sealed class ContractMember
        {
            public ContractMember(string memberName, string returnType, string legacySource)
            {
                MemberName = memberName;
                ReturnType = returnType;
                LegacySource = legacySource;
            }

            public string MemberName { get; }
            public string ReturnType { get; }
            public string LegacySource { get; }
        }
    }
}
