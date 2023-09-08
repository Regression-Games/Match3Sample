using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RegressionGames;
using RegressionGames.Types;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RunBotTest
{

    private string timeNow()
    {
        return $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss:ffff}  [{Thread.CurrentThread.ManagedThreadId}] --- ";
    }
    
    [UnityTest]
    public IEnumerator RunTest()
    {

        Debug.Log($"{timeNow()} Starting test");

        // Override this to change how long a test will wait for bots to join before failing
        const int QUEUE_TIMEOUT_IN_SECONDS = 30;
        const int CONNECT_TIMEOUT_IN_SECONDS = 60;
        const int TEST_RUN_TIMEOUT_IN_SECONDS = 300;

        // For in-editor purposes, feel free to define a default bot to use!
        int defaultBotId = 1000036;

        // NOTE: Make sure to fill in the name of the scene to start your test with!
        Debug.Log($"{timeNow()} Waiting for scene to load...");
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync("Match 3 Starter/Scenes/Game", LoadSceneMode.Single);
        // Wait until the scene finishes loading, then wait a frame so every Awake and Start method is called
        while (!asyncLoadLevel.isDone)
        {
            yield return null;
        }
        yield return null;
        
        Debug.Log($"{timeNow()} Scene loaded");

        // Grab the bot to start (override with the one from CI/CD if defined)
        if (RGEnvConfigs.ReadBotId() != null)
        {
            defaultBotId = Int32.Parse(RGEnvConfigs.ReadBotId());
        }
        int[] botIds = {defaultBotId};
        Debug.Log($"{timeNow()} Loaded config, using bots {string.Join(", ", botIds)}");
        
        // do this before the queue
        RGBotServerListener.GetInstance().StartGame();

        var botInstanceIds = new List<long>();

        // startup all the queue requests
        foreach (var botId in botIds)
        {
            Debug.Log(
                $"{timeNow()} Running task to spawn bot with ID: {botId}");
            var task = RGServiceManager.GetInstance()
                .QueueInstantBot((long) botId, (botInstance) =>
                {
                    botInstanceIds.Add(botInstance.id);
                    RGBotServerListener.GetInstance().AddClientConnectionForBotInstance(botInstance.id);
                }, () =>
                {
                    Debug.LogError($"{timeNow()} Error starting bot with ID {botId}");
                });
            Debug.Log($"{timeNow()} Waiting for bot ID: {botId} to be queued (Completed: {task.IsCompleted}) ...");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            var startTime = DateTime.Now;
            while (!task.IsCompleted &&
                   (DateTime.Now.Subtract(startTime).TotalSeconds < QUEUE_TIMEOUT_IN_SECONDS))
            {
                yield return null;
            }
            
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogWarning($"{timeNow()} Error running task to queue bot id: {botId}\r\n" +
                                 $"Status: {task.Status}\r\n" +
                                 $"AsyncState: {task.AsyncState}\r\n" +
                                 $"Completed: {task.IsCompleted}\r\n" +
                                 $"Canceled: {task.IsCanceled}\r\n" +
                                 $"Faulted: {task.IsFaulted}\r\n" +
                                 $"Exception: {task.Exception}");
                RGBotServerListener.GetInstance()?.StopGame();
                Assert.Fail($"{timeNow()} Bot id: {botId} failed to handle queue request within {QUEUE_TIMEOUT_IN_SECONDS} seconds");
            }
        }

        Debug.Log($"{timeNow()} All bot queue requests sent!");
        
        // Wait until at least one bot is connected. Fail the test if the connection takes too long
        Debug.Log($"{timeNow()} Waiting for bots to connect...");
        var beginTime = DateTime.Now;
        while (!RGBotServerListener.GetInstance().HasBotsRunning() &&
               (DateTime.Now.Subtract(beginTime).TotalSeconds < CONNECT_TIMEOUT_IN_SECONDS))
        {
            yield return null;
        }

        if (!RGBotServerListener.GetInstance().HasBotsRunning())
        {
            Debug.Log($"{timeNow()} Bots failed to connect within {CONNECT_TIMEOUT_IN_SECONDS} seconds");
            RGBotServerListener.GetInstance()?.StopGame();
            Assert.Fail($"{timeNow()} Bots failed to connect within {CONNECT_TIMEOUT_IN_SECONDS} seconds");
        }
        
        Debug.Log($"{timeNow()} Bots connected! Letting them run...");
        RGBotServerListener.GetInstance().SpawnBots();
        // Now run until all bots complete their tasks
        beginTime = DateTime.Now;
        while (RGBotServerListener.GetInstance().HasBotsRunning() &&
               (DateTime.Now.Subtract(beginTime).TotalSeconds < TEST_RUN_TIMEOUT_IN_SECONDS))
        {
            yield return null;
        }

        if (HasFailedValidations(botInstanceIds))
        {
            Debug.Log($"{timeNow()} Bots failed validations\r\n\r\n{GetFailedValidationString(botInstanceIds)}");
            RGBotServerListener.GetInstance()?.StopGame();
            Assert.Fail($"{timeNow()} Bots failed validations\r\n\r\n{GetFailedValidationString(botInstanceIds)}");
        }

        if (RGBotServerListener.GetInstance().HasBotsRunning())
        {
            Debug.Log($"{timeNow()} Bots failed to finish their test run within {TEST_RUN_TIMEOUT_IN_SECONDS} seconds");
            RGBotServerListener.GetInstance()?.StopGame();
            Assert.Fail($"{timeNow()} Bots failed to finish their test run within {TEST_RUN_TIMEOUT_IN_SECONDS} seconds");
        }
        
        Debug.Log($"{timeNow()} Test finished! Cleaning up");
        
        // Cleanup when done
        RGBotServerListener.GetInstance()?.StopGame();
    }

    private string GetFailedValidationString(List<long> botInstanceIds)
    {
        string result = "Failed Bot Validations\r\n=====================\r\n";
        foreach (var botInstanceId in botInstanceIds)
        {
            var failedValidations =
                RGBotServerListener.GetInstance().GetFailedValidationsForClient((uint)botInstanceId);
            if (!failedValidations.IsEmpty)
            {
                string botResult = $"BotInstanceId: {botInstanceId}\r\n";
                foreach (var rgValidationResult in failedValidations)
                {
                    botResult += $" - {rgValidationResult.message}\r\n";
                }
                result += botResult + "----\r\n";
            }
        }

        return result;
    }

    private bool HasFailedValidations(List<long> botInstanceIds)
    {
        foreach (var botInstanceId in botInstanceIds)
        {
            if (!RGBotServerListener.GetInstance().GetFailedValidationsForClient((uint)botInstanceId).IsEmpty)
            {
                return true;
            }
        }

        return false;
    }
    
}