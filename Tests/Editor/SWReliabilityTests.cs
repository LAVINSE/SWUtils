using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SW.Attributes;
using SW.BehaviourTree;
using SW.Data;
using SW.EditorTools.Data;
using SW.Quest;
using SW.StateMachine;
using SW.Stat;
using SW.Util;

namespace SW.Tests
{
    /// <summary>저장 실패, 재진입, 입력 오류와 완료 상태의 회귀를 검사합니다.</summary>
    public sealed class SWReliabilityTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<string> slots = new();
        private string previousSlot;
        private bool previousDiagnosticsEnabled;
        private bool previousLogOutputEnabled;

        /// <summary>현재 슬롯을 보관하고 테스트 전용 슬롯을 선택합니다.</summary>
        [SetUp]
        public void SetUp()
        {
            previousSlot = SWPlayerPrefs.CurrentSlot;
            previousDiagnosticsEnabled = SWEventBus.IsDiagnosticsEnabled;
            previousLogOutputEnabled = SWEventBus.IsLogOutputEnabled;
            SelectNewSlot();
        }

        /// <summary>테스트가 만든 저장값과 객체를 정리하고 이전 슬롯으로 돌아갑니다.</summary>
        [TearDown]
        public void TearDown()
        {
            SWEventBus.ClearAll();
            SWEventBus.IsDiagnosticsEnabled = previousDiagnosticsEnabled;
            SWEventBus.IsLogOutputEnabled = previousLogOutputEnabled;
            foreach (string slot in slots)
            {
                SWPlayerPrefs.SetSlot(slot);
                SWPlayerPrefs.DeleteAll();
            }
            SWPlayerPrefs.SetSlot(previousSlot);
            slots.Clear();
            for (int index = createdObjects.Count - 1; index >= 0; index--)
                if (createdObjects[index] != null) UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            createdObjects.Clear();
        }

        /// <summary>빈 문자열과 구분자가 포함된 키의 저장 왕복을 검사합니다.</summary>
        [TestCase("")]
        [TestCase("저장값")]
        public void StringValuesSurviveExportAndImport(string value)
        {
            SWPlayerPrefs.SetString("chapter|reward", value);
            string serialized = SWPlayerPrefs.ExportToJson();
            SWPlayerPrefs.SetSlot(SWPlayerPrefs.CurrentSlot);
            Assert.That(SWPlayerPrefs.GetString("chapter|reward", "기본값"), Is.EqualTo(value));
            SWPlayerPrefs.DeleteAll();
            Assert.That(SWPlayerPrefs.HasKey("chapter|reward"), Is.False);
            Assert.That(SWPlayerPrefs.ImportFromJson(serialized), Is.True);
            Assert.That(SWPlayerPrefs.GetString("chapter|reward", "기본값"), Is.EqualTo(value));
        }

        /// <summary>잘못된 가져오기가 기존 값을 바꾸지 않는지 검사합니다.</summary>
        [TestCase("{}")]
        [TestCase("{\"entries\":[null]}")]
        [TestCase("{\"entries\":[{\"key\":\"\",\"value\":\"bad\"}]}")]
        [TestCase("{\"entries\":[{\"key\":\"same\",\"value\":\"1\"},{\"key\":\"same\",\"value\":\"2\"}]}")]
        public void InvalidImportPreservesExistingValues(string serialized)
        {
            SWPlayerPrefs.SetString("original", "보존");
            Assert.That(SWPlayerPrefs.ImportFromJson(serialized), Is.False);
            Assert.That(SWPlayerPrefs.GetString("original"), Is.EqualTo("보존"));
            Assert.That(SWPlayerPrefs.MergeFromJson(serialized), Is.False);
            Assert.That(SWPlayerPrefs.GetString("original"), Is.EqualTo("보존"));
        }

        /// <summary>명시한 슬롯의 복원이 현재 슬롯을 바꾸지 않는지 검사합니다.</summary>
        [Test]
        public void ExplicitSlotImportPreservesCurrentSelection()
        {
            string firstSlot = SWPlayerPrefs.CurrentSlot;
            SWPlayerPrefs.SetString("value", "first");
            string serialized = SWPlayerPrefs.ExportToJson();
            string secondSlot = SelectNewSlot();
            SWPlayerPrefs.SetString("value", "second");
            Assert.That(SWPlayerPrefs.ImportFromJson(serialized, firstSlot), Is.True);
            Assert.That(SWPlayerPrefs.CurrentSlot, Is.EqualTo(secondSlot));
            Assert.That(SWPlayerPrefs.GetString("value"), Is.EqualTo("second"));
            Assert.That(SWPlayerPrefs.GetString("value", null, firstSlot), Is.EqualTo("first"));
        }

        /// <summary>기존 키 목록을 읽고 새 키를 추가할 때 새 형식으로 저장하는지 검사합니다.</summary>
        [Test]
        public void LegacyKeyListMigratesOnWrite()
        {
            SWPlayerPrefs.SetString("legacy", "value");
            string slot = SWPlayerPrefs.CurrentSlot;
            PlayerPrefs.DeleteKey("SwUtilsPrefs_KeyIndexV2_" + slot);
            PlayerPrefs.SetString("SwUtilsPrefs_KeyIndex_" + slot, "legacy");
            SWPlayerPrefs.SetSlot(slot);
            Assert.That(SWPlayerPrefs.GetString("legacy"), Is.EqualTo("value"));
            SWPlayerPrefs.SetString("new|key", "second");
            SWPlayerPrefs.SetSlot(slot);
            SWPlayerPrefs.DeleteAll();
            Assert.That(SWPlayerPrefs.HasKey("legacy"), Is.False);
            Assert.That(SWPlayerPrefs.HasKey("new|key"), Is.False);
        }

        /// <summary>파일 교체 실패 시 이전 파일을 보존하고 성공 시 백업을 남기는지 검사합니다.</summary>
        [Test]
        public void AtomicReplacementPreservesPreviousFileWhenBackupFails()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SWUtilsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "save.json");
            string backup = Path.Combine(directory, "backup.json");
            MethodInfo write = typeof(SWSaveDataManager).Assembly.GetType("SW.Data.SWAtomicFile")
                .GetMethod("WriteAllText", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                File.WriteAllText(path, "old");
                Assert.Throws<TargetInvocationException>(() => write.Invoke(null, new object[] { path, "lost", directory }));
                Assert.That(File.ReadAllText(path), Is.EqualTo("old"));
                write.Invoke(null, new object[] { path, "new", backup });
                Assert.That(File.ReadAllText(path), Is.EqualTo("new"));
                Assert.That(File.ReadAllText(backup), Is.EqualTo("old"));
            }
            finally
            {
                foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
                Directory.Delete(directory);
            }
        }

        /// <summary>길이가 0인 타이머가 한 번의 갱신에서 완료되는지 검사합니다.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ZeroDurationCompletesOnce(bool loop)
        {
            SWTimer timer = new(0f, true, loop);
            Assert.That(timer.Tick(0f), Is.True);
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.Tick(1f), Is.False);
            timer.Restart();
            Assert.That(timer.Tick(0f), Is.True);
        }

        /// <summary>타이머 길이를 줄인 뒤 다음 갱신에서 완료되는지 검사합니다.</summary>
        [Test]
        public void ShorteningRunningTimerCompletesOnNextTick()
        {
            SWTimer timer = new(10f, true);
            timer.Tick(5f);
            timer.SetDuration(2f);
            Assert.That(timer.Tick(0f), Is.True);
            Assert.That(timer.Tick(0f), Is.False);
        }

        /// <summary>블랙보드의 빈 참조, 이름 변경과 타입 검사를 확인합니다.</summary>
        [Test]
        public void BlackboardNullAndRenameKeepTypesAndLookupConsistent()
        {
            SWBehaviourBlackboard blackboard = new();
            SWBehaviourBlackboardEntry entry = blackboard.Add("target", SWBehaviourBlackboardValueType.Object);
            GameObject target = new("검증 대상");
            createdObjects.Add(target);
            Assert.That(blackboard.SetValue("target", target), Is.True);
            Assert.That(blackboard.SetValue<UnityEngine.Object>("target", null), Is.True);
            Assert.That(blackboard.TryGetValue("target", out UnityEngine.Object value), Is.True);
            Assert.That(value, Is.Null);
            Assert.That(blackboard.TryGetValue("target", out string _), Is.False);
            entry.Name = "changed";
            Assert.That(blackboard.TryGetEntry("target", out _), Is.False);
            Assert.That(blackboard.TryGetEntry("changed", out _), Is.True);
            blackboard.Add("occupied", SWBehaviourBlackboardValueType.String);
            Assert.That(blackboard.Rename(entry.Identifier, "occupied"), Is.False);
        }

        /// <summary>진단 문자열 변환 실패와 진단 끄기가 이벤트 전달을 막지 않는지 검사합니다.</summary>
        [Test]
        public void EventDiagnosticsCannotInterruptPublishing()
        {
            int delivered = 0;
            SWEventBus.Subscribe<ThrowingText>(value => delivered++);
            SWEventBus.IsLogOutputEnabled = false;
            ThrowingText payload = new();
            Assert.DoesNotThrow(() => SWEventBus.Publish(payload, false));
            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(payload.Calls, Is.EqualTo(1));
            SWEventBus.IsDiagnosticsEnabled = false;
            Assert.DoesNotThrow(() => SWEventBus.Publish(payload, false));
            Assert.That(delivered, Is.EqualTo(2));
            Assert.That(payload.Calls, Is.EqualTo(1));
        }

        /// <summary>정의된 논리값만 적용하고 오타는 거절하는지 검사합니다.</summary>
        [TestCase("truue", false)]
        [TestCase("true", true)]
        [TestCase("false", true)]
        [TestCase("yes", true)]
        [TestCase("0", true)]
        public void TableRejectsUnknownBooleanValues(string value, bool expected)
        {
            TableAsset asset = CreateAsset<TableAsset>();
            SWExcelTableParser.ParseResult parsed = SWExcelTableParser.Parse("Required\tEnabled\nkept\t" + value);
            Assert.That(SWExcelTableParser.ApplyToSheet(asset, SWExcelTableParser.GetSheetFields(asset)[0], parsed), Is.EqualTo(expected));
            if (!expected) Assert.That(asset.Rows[0].Required, Is.EqualTo("original"));
        }

        /// <summary>필수 열이 누락된 표가 원본 에셋을 바꾸지 않는지 검사합니다.</summary>
        [Test]
        public void MissingRequiredColumnDoesNotModifyAsset()
        {
            TableAsset asset = CreateAsset<TableAsset>();
            SWExcelTableParser.ParseResult parsed = SWExcelTableParser.Parse("Enabled\ntrue");
            Assert.That(SWExcelTableParser.ApplyToSheet(asset, SWExcelTableParser.GetSheetFields(asset)[0], parsed), Is.False);
            Assert.That(asset.Rows[0].Required, Is.EqualTo("original"));
        }

        /// <summary>인용된 셀의 탭, 줄바꿈, 따옴표와 빈 문자열을 보존하는지 검사합니다.</summary>
        [TestCase("line one\nline two")]
        [TestCase("left\tright")]
        [TestCase("\"quoted\"")]
        [TestCase("")]
        public void DelimitedValuesRoundTrip(string value)
        {
            string serialized = "key\t" + SWDelimitedText.Quote(value, '\t') + "\r\n";
            List<string[]> rows = SWDelimitedText.Read(serialized);
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "key", value }));
        }

        /// <summary>닫히지 않은 따옴표가 있는 입력을 거절하는지 검사합니다.</summary>
        [Test]
        public void DelimitedTextRejectsUnclosedQuotes()
        {
            Assert.Throws<FormatException>(() => SWDelimitedText.Read("key\t\"unfinished"));
        }

        /// <summary>현재 상태를 제거한 뒤 종료 콜백에서 요청한 상태를 추가하는지 검사합니다.</summary>
        [Test]
        public void StackExitQueuesNewStateAfterRemoval()
        {
            SWStackStateMachine<object> machine = new(new object());
            machine.AddState<ExitPushState>();
            machine.AddState<ReplacementState>();
            machine.Start<ExitPushState>();
            Assert.That(machine.Pop(), Is.True);
            Assert.That(machine.CurrentState, Is.TypeOf<ReplacementState>());
            Assert.That(machine.Count, Is.EqualTo(1));
        }

        /// <summary>보상 실패 후 저장 복원과 재시도에서 성공한 보상을 건너뛰는지 검사합니다.</summary>
        [Test]
        public void RewardRetryAndRestoreDoNotRepeatSuccessfulRewards()
        {
            CountingReward first = CreateAsset<CountingReward>();
            first.name = "first";
            CountingReward second = CreateAsset<CountingReward>();
            second.name = "second";
            second.Fail = true;
            SWQuest quest = CreateWaitingQuest(first, second);
            Assert.That(quest.Complete(), Is.False);
            Assert.That(quest.State, Is.EqualTo(SWQuestState.WaitingForCompletion));
            Assert.That(first.Grants, Is.EqualTo(1));
            SWQuestSaveData saved = quest.CreateSaveData();
            SWQuest restored = CreateWaitingQuest(first, second);
            typeof(SWQuest).GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(restored, new object[] { saved });
            second.Fail = false;
            int completionNotifications = 0;
            restored.StateChanged += (changed, current, previous) => throw new InvalidOperationException("구독자 실패");
            restored.Completed += changed => completionNotifications++;
            Assert.That(restored.Complete(), Is.True);
            Assert.That(first.Grants, Is.EqualTo(1));
            Assert.That(second.Grants, Is.EqualTo(1));
            Assert.That(completionNotifications, Is.EqualTo(1));
            Assert.That(restored.Complete(), Is.False);
        }

        /// <summary>범위 변경에 따른 최종 능력치 알림과 잘못된 범위 거절을 검사합니다.</summary>
        [Test]
        public void StatRangeChangeNotifiesActualValue()
        {
            SWStat stat = CreateAsset<SWStat>();
            stat.DefaultValue = 100f;
            int changes = 0;
            stat.OnValueChanged += (changed, current, previous) => changes++;
            stat.MaxValue = 50f;
            Assert.That(stat.Value, Is.EqualTo(50f));
            Assert.That(changes, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => stat.MinValue = 60f);
            Assert.That(stat.MinValue, Is.EqualTo(0f));
        }

        /// <summary>서로 다른 에셋을 거치는 하위 트리 순환을 검사합니다.</summary>
        [Test]
        public void SubTreeCycleIsDetectedAcrossAssets()
        {
            SWBehaviourTreeAsset first = CreateAsset<SWBehaviourTreeAsset>();
            SWBehaviourTreeAsset second = CreateAsset<SWBehaviourTreeAsset>();
            SWBehaviourNode firstNode = first.AddNode(typeof(SWBehaviourSubTreeNode), Vector2.zero);
            SWBehaviourNode secondNode = second.AddNode(typeof(SWBehaviourSubTreeNode), Vector2.zero);
            SetField(firstNode, "subTreeAsset", second);
            SetField(secondNode, "subTreeAsset", first);
            Assert.That(first.ValidateSubTrees(out _), Is.False);
            SetField(secondNode, "subTreeAsset", null);
            Assert.That(first.ValidateSubTrees(out _), Is.True);
        }

        private string SelectNewSlot()
        {
            string slot = "SWUtilsTest_" + Guid.NewGuid().ToString("N");
            slots.Add(slot);
            SWPlayerPrefs.SetSlot(slot);
            return slot;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private SWQuest CreateWaitingQuest(params SWQuestReward[] rewards)
        {
            SWQuest quest = CreateAsset<SWQuest>();
            SetField(quest, "rewards", rewards);
            typeof(SWQuest).GetProperty("State").SetValue(quest, SWQuestState.WaitingForCompletion);
            return quest;
        }

        private static void SetField(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        /// <summary>표 적용 전후의 데이터를 확인하는 에셋입니다.</summary>
        public sealed class TableAsset : ScriptableObject
        {
            [SWTableSheet("Rows")] public TableRow[] Rows = { new() { Required = "original" } };
        }

        /// <summary>필수 문자열과 논리값을 가진 표 행입니다.</summary>
        [Serializable]
        public sealed class TableRow
        {
            [SWTable("Required", Required = true)] public string Required;
            [SWTable("Enabled")] public bool Enabled;
        }

        /// <summary>지급 전 실패와 성공 횟수를 확인하는 보상입니다.</summary>
        public sealed class CountingReward : SWQuestReward
        {
            public bool Fail;
            public int Grants;
            /// <inheritdoc />
            public override void Grant(SWQuestSystem owner, SWQuest quest)
            {
                if (Fail) throw new InvalidOperationException("지급 전 실패");
                Grants++;
            }
        }

        private sealed class ThrowingText
        {
            public int Calls;
            public override string ToString()
            {
                Calls++;
                throw new InvalidOperationException("문자열 변환 실패");
            }
        }

        private sealed class ExitPushState : SWStackState<object>
        {
            protected override void OnExit() => StateMachine.Push<ReplacementState>();
        }

        private sealed class ReplacementState : SWStackState<object> { }
    }
}
