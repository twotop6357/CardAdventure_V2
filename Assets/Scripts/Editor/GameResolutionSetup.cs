using UnityEditor;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 유니티 에디터의 PlayerSettings 설정을 16:9 (1920x1080) 기준으로 강제 설정하는 에디터 유틸리티입니다.
    /// </summary>
    public static class GameResolutionSetup
    {
        [MenuItem("CardAdventure/Resolution/Enforce 16-9 Aspect Ratio in PlayerSettings")]
        public static void ConfigureResolution()
        {
            // 빌드 시 기본 해상도를 Full HD (1920x1080, 16:9)로 설정
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;

            // 창 모드 시 해상도 관련 옵션 조율
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.forceSingleInstance = true;
            
            // 유저가 창 크기를 드래그하여 임의로 늘려도 런타임의 AspectRatioEnforcer에 의해
            // 찌그러짐 없이 16:9 뷰포트가 고정되고 남는 부분은 검은색 레터박스로 채워집니다.
            PlayerSettings.resizableWindow = true;

            // 에셋 데이터베이스 저장
            AssetDatabase.SaveAssets();

            // 완료 로그 기록
            Debug.Log("<color=green>[CardAdventure]</color> 유니티 PlayerSettings의 기본 빌드 해상도가 1920x1080 (16:9)로 고정되었습니다! 또한, 런타임에 16:9 종횡비를 강제하고 검은색 레터박스/필러박스를 추가하는 [AspectRatioEnforcer]가 작동 중입니다.");
        }
    }
}
