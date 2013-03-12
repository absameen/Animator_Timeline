using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class AnimatorData : MonoBehaviour {
	// show
	public List<AMTake> takes = new List<AMTake>();
	public AMTake playOnStart = null;
	// hide
	
	[HideInInspector] public bool isPlaying {
		get {
			if(nowPlayingTake != null && !isPaused) return true;
			return false;
		}
	}
	[HideInInspector] public string takeName {
		get {
			if(nowPlayingTake != null) return nowPlayingTake.name;
			return null;
		}
	}
	[HideInInspector] public bool isAnimatorOpen = false;
	[HideInInspector] public bool isInspectorOpen = false;
	[HideInInspector] public bool inPlayMode = false;
	[HideInInspector] public float zoom = 0.4f;
	[HideInInspector] public int currentTake;
	[HideInInspector] public int codeLanguage = 0; 	// 0 = C#, 1 = Javascript
	[HideInInspector] public float gizmo_size = 0.05f;
	[HideInInspector] public float width_track = 150f;
	// temporary variables for selecting a property
	//[HideInInspector] public bool didSelectProperty = false;
	//[HideInInspector] public AMPropertyTrack propertySelectTrack;
	//[HideInInspector] public Component propertyComponent;
	//[HideInInspector] public PropertyInfo propertyInfo;
	//[HideInInspector] public FieldInfo fieldInfo;
	[HideInInspector] public bool autoKey = false;
	[HideInInspector] public float elapsedTime = 0f;
	// private
	private AMTake nowPlayingTake = null;
	private bool isPaused = false;
	private bool isLooping = false;
	private float takeTime = 0f;
	
	public object Invoker(object[] args) {
		switch((int)args[0]) {
			// check if is playing
			case 0:
				return isPlaying;
			// get take name
			case 1:
				return takeName;
			// play
			case 2:
				Play((string)args[1],true,0f,(bool)args[2]);
				break;
			// stop
			case 3:
				StopLoop();
				break;
			// pause
			case 4:
				PauseLoop();
				break;
			// resume
			case 5:
				ResumeLoop();
				break;
			// play from time
			case 6:
				Play((string)args[1],false,(float)args[2],(bool)args[3]);
				break;
			// play from frame
			case 7:
				Play((string)args[1],true,(float)((int)args[2]),(bool)args[3]);
				break;
			// preview frame
			case 8:
				PreviewValue((string)args[1],true,(float)args[2]);
				break;
			// preview time
			case 9:
				PreviewValue((string)args[1],false,(float)args[2]);
				break;
			// running time
			case 10:
				if(takeName == null) return 0f;
				else return elapsedTime;
			// total time
			case 11:
				if(takeName == null) return 0f;
				else return (float) nowPlayingTake.numFrames / (float) nowPlayingTake.frameRate;
			case 12:
				if(takeName == null) return false;
				return isPaused;
			default:
				break;
		}
		return null;
	}
	
	void Start() {
		if(playOnStart) {
			Play (playOnStart.name,true,0f,false);
			playOnStart = null;
		}
	}
	
	void OnDrawGizmos() {
		if(!isAnimatorOpen) return;
		takes[currentTake].drawGizmos(gizmo_size, inPlayMode);		
	}
	
	void Update() {
		if(isPaused || nowPlayingTake == null) return;
		elapsedTime += Time.deltaTime;
		if(elapsedTime >= takeTime) {
			nowPlayingTake.stopAudio();
			if(isLooping) Execute(nowPlayingTake);
			else nowPlayingTake = null;
		}
	}
		
	public void Play(string take_name, bool isFrame, float value, bool loop) {
		nowPlayingTake = getTake(take_name);
		if(nowPlayingTake) {
			isLooping = loop;
			Execute (nowPlayingTake, isFrame, value);
		}
	}
	
	public void PreviewValue(string take_name, bool isFrame, float value) {
		AMTake take;
		if(nowPlayingTake && nowPlayingTake.name == takeName) take = nowPlayingTake;
		else take = getTake(take_name);
		if(!take) return;
		float startFrame = value;
		if(!isFrame) startFrame *= take.frameRate;	// convert time to frame
		take.previewFrameInvoker(startFrame);
	}
	
	public void Execute(AMTake take, bool isFrame = true, float value = 0f /* frame or time */) {
		if(nowPlayingTake != null)
			AMTween.Stop();
		// delete AMCameraFade
		float startFrame = value;
		float startTime = value;
		if(!isFrame) startFrame *= take.frameRate;	// convert time to frame
		if(isFrame) startTime /= take.frameRate;	// convert frame to time
		take.executeActions(startFrame);
		elapsedTime = startTime;
		takeTime = (float)take.numFrames/(float)take.frameRate;
		nowPlayingTake = take;
		
	}
	
	public void PauseLoop() {
		if(nowPlayingTake == null) return;
		isPaused = true;
		nowPlayingTake.stopAudio();
		AMTween.Pause();
		
	}
	
	public void ResumeLoop() {
		if(nowPlayingTake == null) return;
		AMTween.Resume();	
		isPaused = false;
	}
	
	public void StopLoop() {
		if(nowPlayingTake == null) return;
		nowPlayingTake.stopAudio();
		nowPlayingTake.stopAnimations();
		nowPlayingTake = null;
		isLooping = false;
		isPaused = false;
		AMTween.Stop();
	}
	
	public int getCurrentTakeValue() {
		return currentTake;	
	}
	
	public int getTakeCount() {
		return takes.Count;	
	}

	public bool setCurrentTakeValue(int _take) {
		if(_take != currentTake) {
			// reset preview to frame 1
			getCurrentTake().previewFrame(1f);
			// change take
			currentTake = _take;
			return true;
		}
		return false;
	}
	
	public AMTake getCurrentTake() {
		return takes[currentTake];	
	}
	
	public AMTake getTake(string takeName) {
		foreach(AMTake take in takes) {
			if(take.name == takeName) return take;	
		}
		Debug.LogError ("Animator: Take '"+takeName+"' not found.");
		return new AMTake(null);
	}
	
	public void addTake() {
		string name = "Take"+(takes.Count+1);
		AMTake a = ScriptableObject.CreateInstance<AMTake>();
		// set defaults
		a.name = name;
		makeTakeNameUnique(a);
		a.frameRate = 24;
		a.numFrames = 1440;
		a.startFrame = 1;
		a.selectedFrame = 1;
		a.selectedTrack = -1;
		a.playbackSpeedIndex = 2;
		//a.lsTracks = new List<AMTrack>();
		//a.dictTracks = new Dictionary<int,AMTrack>();
		a.trackKeys = new List<int>();
		a.trackValues = new List<AMTrack>();
		takes.Add (a);
		selectTake (takes.Count-1);
		
	}
	
	public void deleteTake(int index) {
		//if(shouldCheckDependencies) shouldCheckDependencies = false;
		if(playOnStart == takes[index]) playOnStart = null;
		takes[index].destroy();
		takes.RemoveAt(index);
		if((currentTake>=index)&&(currentTake>0)) currentTake--;
	}
	
	public void deleteCurrentTake() {
		deleteTake (currentTake);	
	}

	public void selectTake(int index) {
		currentTake = index;
	}
	
	public void selectTake(string name) {
		for(int i=0;i<takes.Count;i++)
			if(takes[i].name == name) {
				selectTake (i);
				break;
			}
	}
	public void makeTakeNameUnique(AMTake take) {
		bool loop = false;
		int count = 0;
		do {
			if(loop) loop = false;
			foreach(AMTake _take in takes) {
				if(_take != take && _take.name == take.name) {
					if(count>0) take.name = take.name.Substring(0,take.name.Length-3);
					count++;
					take.name += "("+count+")";
					loop = true;
					break;
				}
			}
		} while (loop);
	}

	public string[] getTakeNames() {
		string[] names = new string[takes.Count+1];
		for(int i=0;i<takes.Count;i++) {
			names[i] = takes[i].name;
		}
		names[names.Length-1] = "Create new...";
		return names;
	}
	
	public int getTakeIndex(AMTake take) {
		for(int i=0;i<takes.Count;i++) {
			if(takes[i] == take) return i;	
		}
		return -1;
	}
	public bool setCodeLanguage(int codeLanguage) {
		if(this.codeLanguage != codeLanguage) {
			this.codeLanguage = codeLanguage;
			return true;
		}
		return false;
	}
	public bool setGizmoSize(float gizmo_size) {
		if(this.gizmo_size != gizmo_size) {
			this.gizmo_size = gizmo_size;
			// update target gizmo size
			foreach(Object target in GameObject.FindObjectsOfType(typeof(AMTarget))) {
				if((target as AMTarget).gizmo_size != gizmo_size) (target as AMTarget).gizmo_size = gizmo_size;
			}
			return true;
		}
		return false;
	}
	
	/*public bool setShowWarningForLostReferences(bool showWarningForLostReferences) {
		if(this.showWarningForLostReferences != showWarningForLostReferences) {
			this.showWarningForLostReferences = showWarningForLostReferences;
			return true;
		}
		return false;
	}*/
	
	public void deleteAllTakesExcept(AMTake take) {
		for(int index=0;index<takes.Count;index++) {
			if(takes[index] == take) continue;
			deleteTake(index);
			index--;
		}
	}
	
	public void mergeWith(AnimatorData _aData) {
		foreach(AMTake take in _aData.takes) {
			takes.Add(take);
			makeTakeNameUnique(take);
		}
	}
	
	public List<GameObject> getDependencies(AMTake _take = null)
	{
		// if only one take
		if(_take != null) return _take.getDependencies().ToList();

		// if all takes
		List<GameObject> ls = new List<GameObject>();
		foreach(AMTake take in takes) {
			ls = ls.Union(take.getDependencies()).ToList();
		}
		return ls;
	}
	
	public List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
		List<GameObject> lsFlagToKeep = new List<GameObject>();
		foreach(AMTake take in takes) {
			lsFlagToKeep = lsFlagToKeep.Union(take.updateDependencies(newReferences,oldReferences)).ToList();
		}
		return lsFlagToKeep;
	}
	
}
