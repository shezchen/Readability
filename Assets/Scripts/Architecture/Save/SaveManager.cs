using System;
using System.IO;
using Architecture.GameSound;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Newtonsoft.Json;

namespace Architecture
{
    public class SaveManager:ManagerNeedInitializeBase
    {
        [Inject] private IAudioService _audioService;
        
        private const string SettingsSaveName = "GameSettings.sav";
        private readonly string _settingsSavePath = Path.Combine(Application.persistentDataPath, SettingsSaveName);

        public bool IsFirstLaunch;
        public GameSettings CurrentSettingsSave;
        public GameSave CurrentGameSave;
        public int CurrentGameSaveIndex;

        /// <summary>
        /// 初始化，会设置IsFirstLaunch和CurrentSettingsSave
        /// </summary>
        public override async UniTask Init()
        {
            await base.Init();
            IsFirstLaunch = !File.Exists(_settingsSavePath);
            if (IsFirstLaunch)
            {
                CurrentSettingsSave = new GameSettings();
                SaveSettings();
            }
            else
            {
                LoadSettings();
                
                _audioService.SetBgmVolume(CurrentSettingsSave.bgmVolume/100f);
                _audioService.SetSfxVolume(CurrentSettingsSave.sfxVolume/100f);

                var resParts = CurrentSettingsSave.gameResolution.ToString().Replace("Res_", "").Split('x');
                if (resParts.Length == 2 && int.TryParse(resParts[0], out int width) && int.TryParse(resParts[1], out int height))
                {
                    Screen.SetResolution(width, height,
                        CurrentSettingsSave.gameWindow == GameWindow.FullScreenWindow
                            ? FullScreenMode.FullScreenWindow
                            : FullScreenMode.Windowed);
                }
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(_settingsSavePath))
            {
                CurrentSettingsSave = new GameSettings();
                Debug.LogWarning("设置数据文件不存在，已创建默认配置。");
                return;
            }

            try
            {
                string json = File.ReadAllText(_settingsSavePath);
                CurrentSettingsSave = JsonConvert.DeserializeObject<GameSettings>(json) ?? new GameSettings();
                Debug.Log("设置数据已从: " + _settingsSavePath + " 加载");
            }
            catch (Exception exception)
            {
                Debug.LogError("设置数据解析失败，已回退默认配置。错误: " + exception.Message);
                CurrentSettingsSave = new GameSettings();
            }
        }

        public void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(CurrentSettingsSave, Formatting.Indented);
            File.WriteAllText(_settingsSavePath, json);
            Debug.Log("设置数据已保存至: " + _settingsSavePath);
        }
        
        public string GetDefaultSavePath(int saveSlot)
        {
            return Path.Combine(Application.persistentDataPath, "SaveSlot" + saveSlot + ".sav");
        }
        
        public async UniTask LoadGame(int index)
        {
            var savePath = GetDefaultSavePath(index);
            if (!File.Exists(savePath))
            {
                Debug.LogError("存档文件不存在: " + savePath);
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(savePath);
                CurrentGameSave = JsonConvert.DeserializeObject<GameSave>(json);
                CurrentGameSaveIndex = index;
                Debug.Log("游戏存档已从: " + savePath + " 加载");
            }
            catch (Exception exception)
            {
                Debug.LogError("存档数据读取失败: " + exception.Message);
            }
        }

        public async UniTask SaveGame(int index)
        {
            var savePath = GetDefaultSavePath(index);
            CurrentGameSave.LastSaveTime = DateTime.Now;
            string json = JsonConvert.SerializeObject(CurrentGameSave, Formatting.Indented);
            await File.WriteAllTextAsync(savePath, json);
            Debug.Log("游戏存档已保存至: " + savePath);
        }
        
        public bool CheckSaveExists(int saveSlot)
        {
            string path = GetDefaultSavePath(saveSlot);
            if (File.Exists(path))
            {
                Debug.Log("存档槽 " + saveSlot + " 存在存档文件。");
                return true;
            }
            else
            {
                Debug.Log("存档槽 " + saveSlot + " 不存在存档文件。");
                return false;
            }
        }
        
        public async UniTask CreateNewSave(int saveSlot)
        {
            CurrentGameSave = new GameSave(0);
            await SaveGame(saveSlot);
            Debug.Log("已在存档槽 " + saveSlot + " 创建新存档文件。");
        }
        
        public bool DeleteSave(int saveSlot)
        {
            string path = GetDefaultSavePath(saveSlot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("存档槽 " + saveSlot + " 的存档文件已删除。");
                return true;
            }
            else
            {
                Debug.LogWarning("存档槽 " + saveSlot + " 的存档文件不存在，无法删除。");
                return false;
            }
        }
    }
}