﻿using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ImageController : MonoBehaviour
{
    //Variables
    private bool isAnyThemeLeftInCurRound;
    private bool pauseActivated;
    private bool isNextAvailable;
    private bool goingToNextQuestion;
    private int numberOfQuestions;
    private int actualQuestion;
    private string localpath;
    //Objects
    private AnswerData currAnswer;
    private Timer timerctrl;
    private Sprite sprite;
    private AudioSource musicSource;
    private AudioClip music;
    private GameObject ans1, ans2, ans3, ans4;
    private GameObject arrow;
    //Arrays
    private Button[] teamsButton;
    private AnswerData[] answers;
    //Lists
    private List<GameObject> teamlist = new List<GameObject>();
    private List<GameObject> answerList = new List<GameObject>();
    private List<GameObject> timerPanelList = new List<GameObject>();
    private List<QuestionData> questions = new List<QuestionData>();
    private List<PlayerModel> teamsctrl = new List<PlayerModel>();

    /*
     * This method initialize everything.
     */
    void Start()
    {
        /*
         * Initialisation of gameobjects and variables
         */

        string[] datapath = Application.dataPath.Split('/');
        string pathsrc = datapath[0] + '/';
        for (int i = 1; i < datapath.Length - 1; i++)
        {
            pathsrc += '/' + datapath[i];
        }
        localpath = pathsrc + "/Images";

        goingToNextQuestion = false;
        pauseActivated = false;
        actualQuestion = 1;
        questions = DataModel.CurTopic().Questions;
        numberOfQuestions = questions.Count;

        arrow = GameObject.Find("ArrowButton");
        teamsButton = GameObject.FindWithTag("teamcontainer").GetComponentsInChildren<Button>();
        musicSource = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>();
       
        Sprite sprite = Resources.Load<Sprite>("Images/" + DataModel.BackgroundName);
        GameObject.Find("Background").GetComponent<Image>().sprite = sprite;

        for (int i = 0; i < teamsButton.Length; i++)
        {
            if (i < DataModel.NumberOfTeams)
            {
                teamsButton[i].GetComponentInChildren<TextMeshProUGUI>().text = DataModel.GetTextScoreFromTeam(i);
            }
            else
            {
                teamsButton[i].gameObject.SetActive(false);
            }
        }
        teamsButton = GameObject.FindWithTag("teamcontainer").GetComponentsInChildren<Button>();


        foreach (GameObject go in GameObject.FindGameObjectsWithTag("team"))
        {
            teamlist.Add(go);
        }
        teamlist = teamlist.OrderBy(go => go.name).ToList();

        foreach (GameObject go in teamlist)
        {
            teamsctrl.Add(go.GetComponent<PlayerModel>());
        }

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("answer"))
        {
            answerList.Add(go);
        }
        answerList = answerList.OrderBy(go => go.name).ToList();

        timerctrl = GameObject.Find("Timer").GetComponent<Timer>();

        foreach (GameObject e in GameObject.FindGameObjectsWithTag("realtimer"))
        {
            timerPanelList.Add(e);
        }
        timerPanelList = timerPanelList.OrderBy(go => go.name).ToList();

        /*
         * Afficher les Jokers qui ont ete actives lors de la scene Topics
         */
        for (int i = 0; i < DataModel.Jokers.Length; i++)
        {
            if (DataModel.Jokers[i])
            {
                GameObject.Find("Joker " + (i + 1)).GetComponent<CanvasGroup>().alpha = 1;
            }
        }

        RunningQuestions();
    }

    void Update()
    {
        // Pause/Resume the game
        if (Input.GetKeyUp(DataModel.Pause))
        {
            if (!pauseActivated)
            {
                //game paused
                Time.timeScale = 0f;
                //disable every controller
                foreach (PlayerModel e in teamsctrl)
                {
                    e.enabled = false;
                }
                //dispay "pause activated" message
                pauseActivated = true;
            }
            else
            {
                //game already in pause i.e. resume game
                Time.timeScale = 1f;
                //enable every controller
                foreach (PlayerModel e in teamsctrl)
                {
                    e.enabled = true;
                }
                //display "resume game" message
                pauseActivated = false;
            }
        }

        //go to next question
        if (Input.GetKeyDown(DataModel.Next))
        {
            GoToNextQuestion();
        }
    }

    private void RunningQuestions()
    {
        goingToNextQuestion = false;
        // Make required objects to disappear at the start of question
        GameObject.Find("ArrowButton").GetComponent<Button>().interactable = false;

        arrow.GetComponent<CanvasGroup>().alpha = 0;
        foreach (GameObject e in timerPanelList)
        {
            e.GetComponent<CanvasGroup>().alpha = 0;
        }
        foreach (GameObject p in answerList)
        {
            p.GetComponent<CanvasGroup>().alpha = 0;
        }

        isNextAvailable = false;

        GameObject.Find("QuestionCounter").GetComponent<TextMeshProUGUI>().text = "Question " + actualQuestion + " / " + numberOfQuestions;

        // Either this is a MusicQuestion and we musicQuestionIsPlaying its music or we musicQuestionIsPlaying the basic question music
        ImageQuestion imageQ = (ImageQuestion)DataModel.CurQuestion();
        var x = new WWW("file:///" + localpath + '/' + imageQ.ImagePath);
        sprite = Sprite.Create(x.texture, new Rect(0, 0, x.texture.width, x.texture.height), new Vector2(0, 0));
        GameObject.Find("Questionimage").GetComponent<Image>().sprite = sprite;
        
        music = (AudioClip)Resources.Load("Sounds/" + DataModel.QuestionMusicName);
        musicSource.clip = music;
        DisableTeam();

        StartCoroutine(DisplayText());

        foreach (PlayerModel e in teamsctrl)
        {
            ChangeTeamColor(0, e);
        }

        musicSource.Play();

        // After 10 seconds, the timer and answers appears, 7 seconds after that a false answer disappears, again 4 seconds after and at 25 sec teams can't answer
        // anymore. Finally at 28 seconds, the true answer is revealed and points are given
        Invoke("RevealAnswers", 10);
        Invoke("EliminateFalseAnswer", 17);
        Invoke("EliminateFalseAnswer", 21);
        Invoke("DisableTeam", 25);
        Invoke("FinalAnswerPhase", 28);
    }

    /**
     * When this method is called, timer and answers appears and players are able to answer
     */
    private void RevealAnswers()
    {
        foreach (GameObject p in answerList)
        {
            p.GetComponent<CanvasGroup>().alpha = 1;
        }
        foreach (GameObject e in timerPanelList)
        {
            e.GetComponent<CanvasGroup>().alpha = 1;
        }
        timerctrl.tickingDown = true;
        EnableTeam();
    }

    /**
     * Eliminate a random false answer of the scene
     */
    private void EliminateFalseAnswer()
    {
        int randomIndex = UnityEngine.Random.Range(0, 4);
        while (answers[randomIndex].IsTrue || answerList[randomIndex].GetComponent<CanvasGroup>().alpha == 0)
        {
            randomIndex = UnityEngine.Random.Range(0, 4);
        }
        answerList[randomIndex].GetComponent<CanvasGroup>().alpha = 0;
    }

    /**
     * Eliminate the last false answer and gives the points to the teams that were right and display the answer's color on the team's answer panel
     */
    private void FinalAnswerPhase()
    {
        EliminateFalseAnswer();
        foreach (PlayerModel e in teamsctrl)
        {
            //change the team's answer button to the color of the one they chose
            ChangeTeamColor(e.GetNumberAnswer(), e);

            for (int i = 0; i < questions.First().Answers.Length; i++)
            {
                currAnswer = questions.First().Answers[i];
                //Check if PlayerControler answered and gave the good answer
                if (e.GetAnswer().Equals(currAnswer.AnswerText) && currAnswer.IsTrue)
                {
                    DataModel.AddScoreToTeam(e.GetCurrentRoundPoints(), teamsctrl.IndexOf(e));
                }
            }
        }

        for (int i = 0; i < teamsButton.Length; i++)
        {
            teamsButton[i].GetComponentInChildren<TextMeshProUGUI>().text = DataModel.GetTextScoreFromTeam(i);
        }
        GameObject.Find("ArrowButton").GetComponent<Button>().interactable = true;

        arrow.GetComponent<CanvasGroup>().alpha = 1;
        isNextAvailable = true;
    }

    private void DisableTeam()
    {
        foreach (PlayerModel e in teamsctrl)
        {
            e.enabled = false;
        }
        if (DataModel.CurQuestion() is MusicQuestion)
        {
            musicSource.Stop();
        }
    }

    private void EnableTeam()
    {
        foreach (PlayerModel e in teamsctrl)
        {
            e.SetHasAnswered(false);
            e.enabled = true;
        }
    }

    /**
     * Change the color of a PlayerControler e based on the integer i which stands for
     * the answer the team chooses. Their color change to the corresponding answer's color.
     */
    private void ChangeTeamColor(int i, PlayerModel e)
    {
        Image img = e.answerPanel.GetComponent<Image>();
        switch (i)
        {
            case 1: //Color of answer number 1
                img.color = new Color(0, 0, 1, 1);
                break;
            case 2: //Color of answer number 2
                img.color = new Color(1, 0.7093226F, 0, 1);
                break;
            case 3: //Color of answer number 3
                img.color = new Color(0.07693553F, 1, 0, 1);
                break;
            case 4: //Color of answer number 4
                img.color = new Color(0.9725245F, 1, 0, 1);
                break;
            default: //White, used when waiting for an answer
                img.color = new Color(1, 1, 1, 1);
                break;
        }
    }//end of changeTeamColor

    /*
     * This method is called once. It is used to display the questions character by character to
     * make it look dynamic. It also displays the answers' text
     */
    private IEnumerator DisplayText()
    {
        while (!goingToNextQuestion)
        {
            answers = questions.First().Answers;
            GameObject.Find("Answer 1").GetComponent<TextMeshProUGUI>().text = answers[0].AnswerText;
            GameObject.Find("Answer 2").GetComponent<TextMeshProUGUI>().text = answers[1].AnswerText;
            GameObject.Find("Answer 3").GetComponent<TextMeshProUGUI>().text = answers[2].AnswerText;
            GameObject.Find("Answer 4").GetComponent<TextMeshProUGUI>().text = answers[3].AnswerText;
            yield return null;
        }
    }

    public void GoToNextQuestion()
    {
        goingToNextQuestion = true;
        if (isNextAvailable)
        {
            questions.Remove(DataModel.CurQuestion());
            actualQuestion++;

            if (!questions.Any())
            {
                isAnyThemeLeftInCurRound = false;
                for (int i = 0; i < DataModel.CurRound().Topics.Count; i++)
                {
                    if (DataModel.CurRound().Topics[i].IsAvailable)
                    {
                        isAnyThemeLeftInCurRound = true;
                    }
                }
                // if there are no more questions or topics, go to next round
                if (!isAnyThemeLeftInCurRound)
                {
                    DataModel.Rounds.Remove(DataModel.CurRound());
                    DataModel.RoundNumber++;
                }
                if (!DataModel.Rounds.Any())
                {
                    SceneManager.LoadScene("Ending");
                }
                //if it is the last question, return to Topics
                else
                {
                    SceneManager.LoadScene("Topics");
                }
            }
            else
            {
                //display next question without answers
                foreach (GameObject go in answerList)
                {
                    go.GetComponent<CanvasGroup>().alpha = 0;
                }

                timerctrl.ResetTimer();

                RunningQuestions();
            }
        }
    }
}