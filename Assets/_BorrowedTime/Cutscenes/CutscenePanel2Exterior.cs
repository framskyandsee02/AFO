using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BorrowedTime.Characters;

/// <summary>
/// Storyboard panel 2: the protagonist comes out of the house and takes the letter from the mailbox.
/// Started by CutsceneMailDelivery.onFinished (wired in the scene).
/// </summary>
public class CutscenePanel2Exterior : MonoBehaviour
{
    public CutsceneMailDelivery p1;          // reuses its fader + skip button
    public Transform hero;
    public BTCharacterModel model;
    public Transform doorHinge;
    public Transform camTf;
    public Transform mailboxStand;           // where the hero stops next to the mailbox
    public Vector3[] waypoints;
    public float walkSpeed = 2.4f;
    public string nextSceneName = "Cutscene02_Interior";
    public string skipToScene = "TrainingArea";

    bool _running, _loading;
    bool holdingLetter;
    float nextStep;

    public void Begin() { if (!_running) { _running=true; StartCoroutine(Run()); } }

    void LateUpdate()
    {
        if(holdingLetter && model.rightHand && p1.letter)
            p1.letter.SetPositionAndRotation(model.rightHand.position, model.rightHand.rotation);
    }

    void Update()
    {
        if (_running && !_loading && p1 != null && p1.Skipped)
        {
            _loading = true;
            SceneManager.LoadScene(skipToScene);
        }
    }

    IEnumerator Run()
    {
        _running = true;
        p1.cameraPushEnabled = false;
        p1.Presentation.SetShot("collect-letter", "มีจดหมายมาถึง... ใครส่งมากันนะ?");

        // new camera shot: side-on to the mailbox, house in the back
        Vector3 camA = new Vector3(3.6f, 2.3f, -9.5f), camB = new Vector3(4.6f, 2.2f, -8.6f);
        Vector3 look = new Vector3(7.6f, 1.4f, 0.5f);
        camTf.position = camA;
        camTf.rotation = Quaternion.LookRotation(look - camA);

        hero.position = waypoints[0];
        hero.rotation = Quaternion.Euler(0, 180f, 0);
        hero.gameObject.SetActive(true);
        model.PlayPose("Idle");
        model.SetFace("Neutral");
        model.autoReturn=false;
        if (doorHinge) doorHinge.localRotation = Quaternion.identity;

        // fade in
        yield return p1.Fade(1f, 0f, 0.9f);

        // door swings open, hero steps out
        float t = 0;
        while (t < 0.9f) { t += Time.deltaTime; Cam(camA, camB, look, t / 4.5f);
            if (doorHinge) doorHinge.localRotation = Quaternion.Euler(0, Mathf.SmoothStep(0, 85f, t / 0.9f), 0); yield return null; }
        yield return Wait(0.3f, camA, camB, look, t);
        t += 0.3f;

        model.PlayPose("Walk");
        for (int i = 1; i < waypoints.Length; i++)
        {
            Vector3 target = waypoints[i];
            while ((hero.position - target).sqrMagnitude > 0.02f)
            {
                Vector3 dir = target - hero.position; dir.y = 0;
                hero.rotation = Quaternion.Slerp(hero.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 7f);
                hero.position = Vector3.MoveTowards(hero.position, target, walkSpeed * Time.deltaTime);
                Footstep();
                t += Time.deltaTime; Cam(camA, camB, look, t / 4.5f);
                yield return null;
            }
        }

        // turn to the mailbox
        model.PlayPose("Idle");
        Quaternion faceMailbox = Quaternion.LookRotation(new Vector3(-1f, 0, -0.15f));
        for (float k = 0; k < 0.5f; k += Time.deltaTime)
        { hero.rotation = Quaternion.Slerp(hero.rotation, faceMailbox, k / 0.5f); t += Time.deltaTime; Cam(camA, camB, look, t / 4.5f); yield return null; }

        // pick up the letter
        model.PlayPose("Pickup");
        p1.Presentation.Cue("letter");
        Transform letter = p1.letter;
        letter.gameObject.SetActive(true);
        letter.localScale = new Vector3(0.5f, 0.04f, 0.36f);
        Vector3 from = p1.mailboxSlot.position;
        for (float k = 0; k < 1.0f; k += Time.deltaTime)
        {
            Vector3 hand = model.rightHand ? model.rightHand.position : hero.position + Vector3.up * 1.0f;
            float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01((k - 0.35f) / 0.65f));
            letter.position = Vector3.Lerp(from, hand, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.25f;
            letter.rotation = Quaternion.Slerp(Quaternion.identity, hero.rotation, e);
            t += Time.deltaTime; Cam(camA, camB, look, t / 4.5f);
            yield return null;
        }
        if (p1.mailboxFlag) p1.mailboxFlag.localRotation = Quaternion.Euler(0, 0, -85f);   // flag drops, mailbox is empty

        // hero holds the letter, then follows the hand with it
        model.SetFace("Smile");
        holdingLetter=true;
        p1.Presentation.SetShot("return-home", "กลับไปอ่านข้างในดีกว่า");
        for (float k = 0; k < 1.6f; k += Time.deltaTime)
        {
            if (model.rightHand) { letter.position = model.rightHand.position; letter.rotation = model.rightHand.rotation; }
            t += Time.deltaTime; Cam(camA, camB, look, t / 4.5f);
            yield return null;
        }

        model.PlayPose("Walk");
        for(int i=waypoints.Length-2;i>=0;i--)
        {
            while((hero.position-waypoints[i]).sqrMagnitude>.02f)
            {
                Vector3 direction=waypoints[i]-hero.position;direction.y=0;
                if(direction.sqrMagnitude>.001f)hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(direction),Time.deltaTime*7);
                hero.position=Vector3.MoveTowards(hero.position,waypoints[i],walkSpeed*Time.deltaTime);
                Footstep();yield return null;
            }
        }
        model.PlayPose("Idle");
        for(float k=0;k<.6f;k+=Time.deltaTime)
        { if(doorHinge)doorHinge.localRotation=Quaternion.Euler(0,Mathf.Lerp(85,0,k/.6f),0);yield return null; }

        yield return p1.Fade(0f, 1f, 0.8f);
        _loading = true;
        SceneManager.LoadScene(nextSceneName);
    }

    void Footstep()
    { if(Time.time>=nextStep){nextStep=Time.time+.42f;p1.Presentation.Cue("step");} }

    IEnumerator Wait(float s, Vector3 a, Vector3 b, Vector3 look, float t0)
    {
        for (float k = 0; k < s; k += Time.deltaTime) { Cam(a, b, look, (t0 + k) / 4.5f); yield return null; }
    }

    void Cam(Vector3 a, Vector3 b, Vector3 look, float k)
    {
        camTf.position = Vector3.Lerp(a, b, Mathf.Clamp01(k));
        camTf.rotation = Quaternion.LookRotation(look - camTf.position);
    }
}
