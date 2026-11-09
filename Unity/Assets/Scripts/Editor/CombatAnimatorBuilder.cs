using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WhereDarknessDwells.Game;

namespace WhereDarknessDwells.Editor
{
    /// <summary>
    /// Builds the animator the combat stage's fighters use, from Synty's Sword Combat and Base Locomotion clips: an idle
    /// loop and the actions <see cref="CombatStage.Act"/> plays (attacks, a gesture for other cards, flinches, a raised
    /// guard and a fall). The clips are licensed, so the controller goes to LocalArt (gitignored), and the stage prefab
    /// there is pointed at it. Without the packs, the stage keeps its idle-only controller and nothing else changes.
    /// </summary>
    public static class CombatAnimatorBuilder
    {
        const string ControllerPath = "Assets/LocalArt/Combat/CombatActions.controller";
        const string StagePath = "Assets/LocalArt/Resources/CombatStage.prefab";
        const float Blend = 0.12f;

        [MenuItem("Where Darkness Dwells/Build Combat Animator")]
        public static void Build()
        {
            AnimationClip idle = Clip("A_Idle_Standing_Masc");
            if (idle == null || Clip("A_Hit_F_React_Sword") == null)
            {
                Debug.LogWarning("[Editor] The Synty Sword Combat and Base Locomotion packs aren't in Assets/Synty, so there is no combat animator to build. Fighters keep their idle loop.");
                return;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState rest = machine.AddState(CombatStage.IdleState);
            rest.motion = idle;
            machine.defaultState = rest;

            // Each action plays once and settles back into the idle loop.
            AnimatorState Once(string name, string clip, AnimatorState then = null)
            {
                AnimatorState state = machine.AddState(name);
                state.motion = Clip(clip);
                AnimatorStateTransition back = state.AddTransition(then ?? rest);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.duration = 0.15f;
                return state;
            }

            for (int i = 0; i < CombatStage.AttackClips.Length; i++) Once(CombatStage.AttackState + (i + 1), CombatStage.AttackClips[i]);
            Once(CombatStage.CastState, "A_Idle_Flourish01_Sword");
            Once(CombatStage.HitState, "A_Hit_F_React_Sword");
            Once(CombatStage.StaggerState, "A_Hit_F_Stagger_Sword");
            Once(CombatStage.GuardState, "A_Block_Begin_Sword", Once(CombatStage.GuardState + " End", "A_Block_End_Sword"));

            // A fallen fighter stays down: the fall ends in a held pose with no way back. Both fall where the fighter
            // stands rather than travelling across the stage.
            InPlace("A_Death_F_01_Sword");
            InPlace("A_Death_F_01_Pose_Sword");
            AnimatorState lying = machine.AddState(CombatStage.DeathState + " Pose");
            lying.motion = Clip("A_Death_F_01_Pose_Sword");
            Once(CombatStage.DeathState, "A_Death_F_01_Sword", lying);

            string missing = string.Join(", ", machine.states.Where(s => s.state.motion == null).Select(s => s.state.name));
            AssetDatabase.SaveAssets();

            GameObject stage = PrefabUtility.LoadPrefabContents(StagePath);
            bool staged = stage != null;
            if (staged)
            {
                var fields = new SerializedObject(stage.GetComponent<CombatStage>());
                fields.FindProperty("idle").objectReferenceValue = controller;
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(stage, StagePath);
                PrefabUtility.UnloadPrefabContents(stage);
            }
            Debug.Log($"[Editor] Built {ControllerPath}" + (staged ? " and gave it to the combat stage." : $", but {StagePath} is missing.") +
                (missing.Length > 0 ? $" Clips not found for: {missing}." : ""));
        }

        /// <summary>The animation in a Synty clip file, found by its name anywhere under Assets/Synty.</summary>
        static AnimationClip Clip(string name)
        {
            string path = ClipPath(name);
            return path == null ? null : AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        static string ClipPath(string name) => AssetDatabase.FindAssets(name, new[] { "Assets/Synty" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == name);

        /// <summary>
        /// Stops a clip carrying the body across the floor. Synty bakes the hips' sideways travel into the pose, measured
        /// from the rig's root bone, so a fall would land a couple of metres away. Measured from the body's centre of mass
        /// and left as root motion instead, the travel is dropped by the stage, which applies none.
        /// </summary>
        static void InPlace(string name)
        {
            if (!(AssetImporter.GetAtPath(ClipPath(name) ?? "") is ModelImporter importer)) return;
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.All(c => !c.lockRootPositionXZ && !c.keepOriginalPositionXZ)) return;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.lockRootPositionXZ = false;
                clip.keepOriginalPositionXZ = false;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }
}
