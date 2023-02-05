using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPerformanceManager : Singleton<PlayerPerformanceManager>
{
    [SerializeField] private float flashSpeed;
    [SerializeField] private float earlyTimingWindowBeats;
    [SerializeField] private float lateTimingWindowBeats;
    //[SerializeField, Range(0f, 1f)] private float failurePercentage;
    [SerializeField] private int playerMaxHealthPerSection;

    private int hitNotesInSection;
    private int missedNotesInSection;    //Letting the note pass without hitting it.
    private int missHits;    //Pressing button at wrong time

    private int playerHealth;

    private bool failed = false;


    private HashSet<SpriteRenderer> flashing = new HashSet<SpriteRenderer>();

    public void StartNewSection()
    {
        failed = false;
        hitNotesInSection = 0;
        missedNotesInSection = 0;
        playerHealth = playerMaxHealthPerSection;
    }

    private void Update()
    {
        if (CheckFailure() && !failed)
        {
            failed = true;
            Debug.Log("You Failed n00b");
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            HandlePlayedNote(0);
        } 
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            HandlePlayedNote(1);
        } 
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            HandlePlayedNote(2);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            HandlePlayedNote(3);
        }
    }

    public bool CheckFailure()
    {
        return playerHealth <= 0;
    }

    private void HandlePlayedNote(int pitch)
    {
        TrackNote note = CheckHitNote(pitch);
        Debug.Log($"Player Pressed Note: {pitch}");

        if (note == null)
        {
            //MISSED
            HandleMissHit();

        } else
        {
            //HIT
            HandleNoteHit(note);
        }
    }

    public void HandleNoteHit(TrackNote note)
    {
        //Do VFX Things. Keep Track of Pass/Fail, etc.
        hitNotesInSection++;

        StartCoroutine(FlashColor(note.GetComponent<SpriteRenderer>(), Color.green, flashSpeed, () =>
        {
            Track.Instance.ActiveNotes.Remove(note);
            Destroy(note.gameObject);
        }));
    }

    public void HandleMissHit()
    {
        missHits++;
        playerHealth--;
        StartCoroutine(FlashColor(Track.Instance.BeatBar.GetComponent<SpriteRenderer>(), Color.red, flashSpeed));
    }

    public void HandleNoteMissed()
    {
        //Do VFX Things. Keep Track of Pass/Fail, etc.
        missedNotesInSection++;
        playerHealth--;
    }

    private IEnumerator FlashColor(SpriteRenderer sr, Color c, float duration, System.Action callback = null)
    {
        if (!flashing.Contains(sr))
        {
            flashing.Add(sr);
            Color ogColor = sr.color;
            sr.color = c;
            yield return new WaitForSeconds(duration);
            sr.color = ogColor;
            flashing.Remove(sr);
        }

        if (callback != null)
        {
            callback();
        }
    }

    private TrackNote CheckHitNote(int pitch)
    {
        List<TrackNote> activeNotes = Track.Instance.ActiveNotes;

        TrackNote hitNote = null;
        foreach (TrackNote note in activeNotes)
        {
            if (pitch == note.NoteData.pitch)
            {
                int noteMoment = Track.Instance.NoteMomentOnTrackCU(note.NoteData);

                int earlyTimingWindowCU = Conductor.Instance.BeatsToCU(earlyTimingWindowBeats);
                int lateTimingWindowCU = Conductor.Instance.BeatsToCU(lateTimingWindowBeats);

                if (noteMoment > earlyTimingWindowCU) {
                    //We've reached the point where notes haven't reached the early window yet and can't be hit.
                    break;
                }

                if (noteMoment <= earlyTimingWindowCU && noteMoment >= -lateTimingWindowCU)
                {
                    hitNote = note;
                    break;
                }
            }

        }

        return hitNote;
    }
}