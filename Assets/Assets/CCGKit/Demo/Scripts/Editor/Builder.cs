// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEditor;

namespace CCGKit
{
    /// <summary>
    /// Editor utility class to help manage the different builds of the project.
    /// </summary>
    public class Builder
    {
        private static readonly BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        private static readonly BuildOptions buildOptions = BuildOptions.Development;
    
        [MenuItem("Tools/Dedicated Server Kit/Build game server", false, 100)]
        public static void BuildGameServer()
        {
            var levels = new string[] {
                "Assets/CCGKit/Demo/Scenes/GameServer.unity",
                "Assets/CCGKit/Demo/Scenes/Game.unity"
            };
            BuildPipeline.BuildPlayer(levels, "Builds/GameServer", buildTarget, buildOptions);
        }
    
        [MenuItem("Tools/Dedicated Server Kit/Build game client", false, 100)]
        public static void BuildGameClient()
        {
            var levels = new string[] {
                "Assets/CCGKit/Demo/Scenes/Home.unity",
                "Assets/CCGKit/Demo/Scenes/Lobby.unity",
                "Assets/CCGKit/Demo/Scenes/DeckBuilder.unity",
                "Assets/CCGKit/Demo/Scenes/Game.unity"
            };
            BuildPipeline.BuildPlayer(levels, "Builds/GameClient", buildTarget, BuildOptions.None);
        }
    
        [MenuItem("Tools/Dedicated Server Kit/Build all", false, 50)]
        public static void BuildAll()
        {
            BuildGameServer();
            BuildGameClient();
        }
    }
}