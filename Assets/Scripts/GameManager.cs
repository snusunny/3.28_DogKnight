using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour, Subject
{
    // 1. Singleton Pattern: Instance() method
    private static GameManager _instance;
    public static GameManager Instance() {
        if(_instance == null) _instance = FindObjectOfType<GameManager>();
        return _instance;
    }

    // 초기화 설정 바꾸지 말 것
    public int _gameRound = 0;
    public string _whoseTurn = "Player"; //team turn (Player or Enemy)
    public bool _isEnd = false;

    // delegate: TurnHandler, FinishHandler 선언
    public delegate void TurnHandler(int round, string turn, Character character);
    public delegate void FinishHandler(bool end);
    public delegate void TurnEndHandler();
    public TurnHandler TurnEvent;
    public FinishHandler FinishEvent;
    public TurnEndHandler TurnEndEvent;

    public List<Character> players = new List<Character>();
    public List<Character> enemies = new List<Character>();
    public int turn = -1; //member turn

    public List<Character> deadPlayers = new List<Character>();
    public List<Character> deadEnemies = new List<Character>();

    /// <summary>
    /// 2. RoundNotify:
    /// 1) 현재 턴이 Enemy이면 다음 gameRound로
    ///  + Debug.Log($"GameManager: Round {gameRound}.");
    /// 2) TurnNotify() 호출
    /// </summary>
    public void RoundNotify(Action<Character> attack) //스켈레톤이 공격과 턴 체인지를 통합시켰는데, 이를 분리하려니 희대의 스파게티 코드가 탄생... 
    {
        if (_isEnd) return;
        if (!PlayerTurn() && turn >= enemies.Count - 1) {
            _gameRound++;
            Debug.Log($"GameManager: Round {_gameRound}.");
        }
        
        TurnNotify(attack);
    }

    public void RoundNotify() {
        RoundNotify(c => c.Attack(null));
    }

    /// <summary>
    /// 3. TurnNotify:
    /// 1) whoseTurn update
    ///  + Debug.Log($"GameManager: {_whoseTurn} turn.");
    /// 2) _turnHandler 호출
    /// </summary>
    public void TurnNotify(Action<Character> attack)
    {
        turn++;
        if(turn >= TeamMember(PlayerTurn()).Count) {
            SetTurn(!PlayerTurn());
            turn = 0;
            Debug.Log($"GameManager: {_whoseTurn} team's turn.");
        }

        if (CurrentCharacter().dead) {
            RoundNotify();//move to next character
        }
        else {
            TurnEvent(_gameRound, _whoseTurn, CurrentCharacter());
            attack(CurrentCharacter());
        }
    }

    public void TurnNotify() {
        TurnNotify(c => c.Attack(null));
    }

    public void TurnEnd() {
        TurnEndEvent();
    }

    /// <summary>
    /// 4. EndNotify: 
    /// 1) isEnd update
    ///  + Debug.Log("GameManager: The End");
    ///  + Debug.Log($"GameManager: {_whoseTurn} is Win!");
    /// 2) _finishHandler 호출
    /// </summary>
    public void EndNotify()
    {
        //first, check if each team has a survivor
        bool plw = enemies.Count <= 0, enw = players.Count <= 0;

        if (!plw && !enw) return;

        string t = plw ? "Player" : "Enemy";
        _isEnd = true;
        Debug.Log("GameManager: The End");
        if(plw && enw) Debug.Log("GameManager: Tie!");
        else Debug.Log($"GameManager: {t} team won!");

        FinishEvent(_isEnd);
    }

    public void DeadNotify(Character c) {
        if (!c.dead) c.dead = true;
        if (players.Contains(c)) {
            players.Remove(c);
            deadPlayers.Add(c);
        }
        else {
            enemies.Remove(c);
            deadEnemies.Add(c);
        }
    }

    // 5. AddCharacter: _turnHandler, _finishHandler 각각에 메소드 추가
    public void AddCharacter(Character character, bool playerTeam)
    {
        TurnEvent += character.TurnUpdate;
        FinishEvent += character.FinishUpdate;
        if(playerTeam) players.Add(character);
        else enemies.Add(character);

        UI.AddCharacterUI(character, !playerTeam);
    }

    // enum으로 갈아치우고 싶은 마음을 다스리며.
    public bool PlayerTurn() {
        return _whoseTurn != "Enemy";
    }

    public void SetTurn(bool player) {
        _whoseTurn = player ? "Player" : "Enemy";
    }

    public int Round() {
        return _gameRound;
    }

    public Character CurrentCharacter() {
        return PlayerTurn() ? players[turn] : enemies[turn];
    }

    public bool NextPlayerTurn() {
        if (turn >= TeamMember(PlayerTurn()).Count - 1) {
            if (TeamMember(!PlayerTurn()).Count == 0) return PlayerTurn();
            return !PlayerTurn();
        }
        return PlayerTurn();
    }

    public Character NextCharacter() {
        if (turn >= TeamMember(PlayerTurn()).Count - 1) {
            if(TeamMember(!PlayerTurn()).Count == 0) return TeamMember(PlayerTurn())[0];
            return TeamMember(!PlayerTurn())[0];
        }
        return TeamMember(PlayerTurn())[turn + 1];
    }

    public List<Character> TeamMember(bool player) {
        return player ? players : enemies;
    }

    public List<Character> DeadMember(bool player) {
        return player ? deadPlayers : deadEnemies;
    }
}
