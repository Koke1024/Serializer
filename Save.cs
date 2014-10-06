using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Save{
	public class Serialize {
		// <!!> T is any struct or class marked with [Serializable]
		public static void Save<T> (string prefKey, T serializableObject) {
			MemoryStream memoryStream = new MemoryStream ();
			BinaryFormatter bf = new BinaryFormatter ();
			bf.Serialize (memoryStream, serializableObject);
			string tmp = System.Convert.ToBase64String (memoryStream.ToArray ());
			PlayerPrefs.SetString ( prefKey, tmp );
			Debug.Log ("Saved [Key:" + prefKey + "]");
		}
	
		public static T Load<T> (string prefKey) {
			if (!PlayerPrefs.HasKey(prefKey)) return default(T);
			BinaryFormatter bf = new BinaryFormatter();
			string serializedData = PlayerPrefs.GetString(prefKey, "NoData");
			MemoryStream dataStream = new MemoryStream(System.Convert.FromBase64String(serializedData));
			T deserializedObject = (T)bf.Deserialize(dataStream);
			Debug.Log ("Loaded [Key:" + prefKey + "]");
			return deserializedObject;
		}
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chara : StackFunction
{
	static public bool ult;
	static public GameObject targetFrom;
	public GameObject children;
	public Clicker clicker;
	public int row;
	public int col;
	public Popper popper;
	[System.Serializable]
	public struct StrParam
	{
		public int charaID;
		public int strength;
		public int agility;
		public int intelligence;
		public int customPoint;
		public Attribute attr;
		public bool isEnemy;
		public int[] actionID;
		public EnumWeaponType weaponType;
		
		public int attack {
			get {
				switch (attr) {
				case Attribute.STRENGTH:
					return strength * 10;
				case Attribute.AGILITY:
					return agility * 10;
				case Attribute.INTELLIGENCE:
					return intelligence * 10;
				}
				return 0;
			}
		}
		public int speed {
			get {
				return 500 + agility * 6;
			}
		}
		public int life {
			get {
				return 200 + strength * 50;
			}
		}
		public int magic {
			get {
				return intelligence * 3;
			}
		}
	}
	public struct StrAttack
	{
		public int pow;
		public Attribute attr;
		public GameObject whose;
		public GameObject whom;
		public TargetDiv targetDiv;
		public EnumBuff buff;
		public StrBuff buffParam;
		public enum TargetDiv
		{
			DIRECT,
			ALL,
			FAINT,
			REVIVE,
			HEAL,
		}
	}
	public enum EnumWeaponType
	{
		KNUCKLE,
		SHORT_SWORD,
		SWORD,
		SPEAR,
		AX,
		GUN,
		BOOK,
		MAX
	}
	public enum EnumBuff
	{
		ARMOR,
		POISON,
		PROVOKE,
		FREEZE,
		CLASH,
		PHANTOM,
		PARALYSIS,
		BOMB,
		REFLECT,
		FLYING,
		HIDE,
		STUN,
		TIRED,
		MANA,
		FIRE,
		BLOOD,
		PROTECTION,
		HAWK_EYE,
		ARCHER,
		FIGHTER,
		GENERAL,
		KING,
		MAGE,
		PALADIN,
		THIEF,
		TROUBADOUR,
		BARD,
		REGRET,
		BOLERO,
		REQUIEM,
		SERENADE,
		MAX
	}

	public enum EnumPanelState{
		DEFAULT,
		SKILL,
		ITEM,
		MAX
	}

	EnumPanelState panelState = EnumPanelState.DEFAULT;
	
	public struct StrBuff
	{
		public float time;
		public object param;
	}
	public Dictionary<EnumBuff, StrBuff> buffs = new Dictionary<EnumBuff, StrBuff> ();
	
	public enum Attribute
	{
		STRENGTH,
		AGILITY,
		INTELLIGENCE,
		MAX
	}
	
	public enum EnumAction
	{
		COMMON_ATTACK = 0,
		COMMON_DEFENSE,
		COMMON_ULTIMATE,
		COMMON_BENCH,
		COMMON_FORMATION,
		AX,
		BOMB,
		DRAIN,
		PROVOKE,
		HEAL_SELF,
		ARMOR = 10,
		POSION_SLASH,
		METEOR,
		PARALYSIS,
		FLY,
		SLASH,
		REFLECT,
		HIDE,
		FREEZE,
		CLASH,
		PHANTOM = 20,
		FIRE,
		BLOOD,
		PROTECTION,
		HAWK_EYE,
		ARCHER,
		FIGHTER,
		GENERAL,
		KING,
		MAGE,
		PALADIN = 30,
		THIEF,
		TROUBADOUR,
		BARD,
		SERENADE,
		BOLERO,
		REGRET,
		REQUIEM,
		SUMMON,
		SUMMONER,
		COMMON_SKILL,
		COMMON_ITEM,
		COMMON_SKILL_CLOSE,
		COMMON_WAIT,
		MAX
	}
	
	public static Color[] attrColor = {
		Color.white,
		Color.red,
		Color.blue,
		Color.yellow,
		Color.black
	};
	
	public struct StrAction
	{
		public int actionID;
		public string name;
		public string detail;
		public delStackFunction func;
		public int cost;
		public bool passive;
		public StrAction (int _id, string _name, string _detail, delStackFunction _func, int _cost, bool _passive)
		{
			actionID = _id;
			name = _name;
			detail = _detail;
			func = _func;
			cost = _cost;
			passive = _passive;
		}
	}
	
	public AudioClip[] sounds;
	
	public StrParam param;
	public GUIGauge lifeGauge;
	public GUIGauge moveGauge;
	public GUIGauge attackGauge;
	public GameObject texture;
	public GameObject texture2;
	public GameObject text;
	public GameObject lifeBG;
	public int hitCount;
	
	public Button button;
	protected int life;
	protected float moving;
	protected float movePoint;
	public float size = 128;
	float normalAttackWait;
	bool waiting;
	int mana = 0;
	public int Mana {
		get {
			return mana;
		}
		set {
			if (!param.isEnemy) {
				param.strength += 1;
				param.agility += 1;
				param.intelligence += 1;
				mana += 1;
				lifeGauge.Init (param.life, life);
			}
		}
	}
	
	public Menu menu;
	
	public StrAction[] actionList;
	
	// Use gameObject for initialization
	void Start ()
	{
		clicker.SetFunction (ActSet);
		popper.SetFunction (TargetSet);
	}
	
	virtual public void TargetSet (object obj)
	{
		Charas.instance.SetTargetFriend (gameObject);
	}
	
	virtual public void ActSet (object obj)
	{
		Charas.instance.SetActionChara (gameObject);
	}
	
	virtual public void Init (StrParam setParam, int a, int b, int c)
	{

		param = setParam;
		life = param.life;
		int maxParam = Mathf.Max(Mathf.Max(param.strength, param.agility), param.intelligence);
		if(maxParam == param.strength){
			param.attr = Chara.Attribute.STRENGTH;
		}
		else if(maxParam == param.agility){
			param.attr = Chara.Attribute.AGILITY;
		}
		else if(maxParam == param.intelligence){
			param.attr = Chara.Attribute.INTELLIGENCE;
		}
		
		movePoint = 500.0f;
		
		ActionInit (a, b, c);
		
		foreach (int actionID in param.actionID) {
			if(actionList[actionID].passive){
				actionList[actionID].func(null);
			}
		}
		if(param.weaponType == EnumWeaponType.BOOK){
			param.intelligence += 3;
		}

		if(!param.isEnemy){
			texture.guiTexture.texture = ButtonManager.GetTexture (param.charaID);
		}
		else{
			texture.guiTexture.texture = ButtonManager.GetEnemyTexture (param.charaID);
		}
		if (texture2) {
			texture2.guiTexture.texture = ButtonManager.GetFaceTexture (param.charaID);
		}
		lifeGauge.Init (life, life);
		moveGauge.Init (1000, 0);
		lifeGauge.SetColor (Color.red);
		moveGauge.SetColor (Color.blue);
		if (attackGauge) {
			attackGauge.Init (GetNormalAttackWait (), 0);
			attackGauge.SetColor(Color.green);
		}
		text.guiText.text = life + "/" + param.life;
		
		//menu.AddMenu (PanelManager.Create(actions[0].name, Kill, transform.position));
	}
	
	public void Moving (float moveTime)
	{
		moving = moveTime / 10.0f;
		Charas.instance.moving = moving;
	}
	
	void Kill (object obj)
	{
		StrAttack attack;
		SetAttackParam (out attack);
		attack.pow *= 10;
		Damage (attack);
	}
	
	float GetAddAttackPoint ()
	{
		float addMove = (float)param.speed;
		if (HasBuff (EnumBuff.FREEZE) || HasBuff (EnumBuff.STUN) || HasBuff (EnumBuff.ARMOR)) {
			addMove = 0;
		} else {
			if (HasBuff (EnumBuff.PARALYSIS)) {
				addMove *= 0.5f;
			}
			if (HasBuff (EnumBuff.TIRED)) {
				addMove *= 0.5f;
			}
			if (HasBuff (EnumBuff.FLYING)) {
				addMove *= (2.0f + mana);
			}
			if (HasBuff (EnumBuff.REGRET)) {
				addMove *= GetBuffParam<float>(EnumBuff.REGRET);
			}
		}
		return addMove;
	}
	
	float GetAddMagicPoint ()
	{
		float addMove = (float)param.magic;
		if (HasBuff (EnumBuff.MAGE)) {
			addMove *= 1.5f;
		}
		if (HasBuff (EnumBuff.FREEZE) || HasBuff (EnumBuff.STUN)) {
			addMove = 0;
		} else {
			if (HasBuff (EnumBuff.PARALYSIS)) {
				addMove *= 0.5f;
			}
			if (HasBuff (EnumBuff.TIRED)) {
				addMove *= 0.5f;
			}
			if (HasBuff (EnumBuff.FLYING)) {
				addMove *= (2.0f + mana);
			}
		}
		return addMove;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (attackGauge) {
			//attackGauge.Init(GetNormalAttackWait(), );
		}
		if (!Live ()) {
			return;
		}
		StackUpdate ();
		AlignmentUpdate ();
		moveGauge.SetNowValue (movePoint);
		if (Charas.instance.actChara && Charas.instance.actChara != gameObject) {
			//return;
		}
		moving -= Time.deltaTime * Charas.instance.gameSpeed;
		if (Charas.instance.moving > 0) {
			//return;
		}
		//if(moving <= 0 && Live ()){
		if (Live ()) {
			if (HasBuff (EnumBuff.FIRE)) {
				StrAttack fireAttack;
				SetAttackParam (out fireAttack);
				fireAttack.pow = 3;
				Damage (fireAttack);
				if (Random.Range (0, 1.0f) < GetBuffParam<float> (EnumBuff.FIRE)) {
					CureBuff (EnumBuff.FIRE);
				}
			}
			float addAttack = GetAddAttackPoint ();
			float addMP = GetAddMagicPoint ();
			movePoint += addMP * Time.deltaTime * Charas.instance.gameSpeed;
			if (Charas.instance.actChara == gameObject && menu && panelState == EnumPanelState.SKILL) {
				for (int i = 0; i < menu.MenuCount(); ++i) {
					if (movePoint - addMP * Time.deltaTime * Charas.instance.gameSpeed < actionList [param.actionID [i]].cost && movePoint >= actionList [param.actionID [i]].cost) {
						CreatePanels (EnumPanelState.DEFAULT);
						break;
					}
				}
			}
			//if(Charas.instance.actChara == null){
			normalAttackWait += addAttack * Time.deltaTime * Charas.instance.gameSpeed;
			if (attackGauge) {
				//attackGauge.SetNowValue (normalAttackWait);
				attackGauge.Init(GetNormalAttackWait (), normalAttackWait);
			}
			if (normalAttackWait > GetNormalAttackWait ()) {
				normalAttackWait = 0;
				attackGauge.Init(GetNormalAttackWait (), 0);
				//MoveStart(null);
				NormalAttack (null);
				//MoveEnd (null);
			}
			//}
			if (movePoint >= 1000 && !Charas.instance.stop) {
				CureBuff (EnumBuff.TIRED);
				movePoint = 1000;
				moveGauge.SetColor(Color.Lerp (Color.blue, Color.white, 0.3f));
				if(!waiting && !param.isEnemy){
					Debug.Log ("俺のターン");
					if(Charas.instance.actChara == null){
						Charas.instance.SetActionChara(gameObject);
					}
					Charas.instance.Stop(true);
				}
			}
		}
		//if(!Charas.instance.showReserve){
		//}
		
		//if(Charas.instance.actChara == null){
		List<EnumBuff> buffList = new List<EnumBuff> (buffs.Keys);
		foreach (EnumBuff buff in buffList) {
			StrBuff newBuff = buffs [buff];
			if(newBuff.time < 500.0f){
				newBuff.time -= Time.deltaTime * Charas.instance.gameSpeed;
			}
			if (buff == EnumBuff.REQUIEM) {
				if(newBuff.time < (float)newBuff.param){
					newBuff.param = (float)newBuff.param - 0.5f;
					Heal((int)(param.life * 0.02f));
				}
			}
			buffs [buff] = newBuff;
			if (buffs [buff].time <= 0) {
				if (buff == EnumBuff.PHANTOM) {
					StrAttack attack;
					SetAttackParam (out attack);
					attack.pow = 9999999;
					Damage (attack);
				}
				if (buff == EnumBuff.BOMB && GetAddAttackPoint () > 0) {
					Bomber (null);
				}
				CureBuff (buff);
			}
		}
		//}
	}
	
	virtual public void AlignmentUpdate ()
	{
		if (Charas.instance.actChara && Charas.instance.actChara != gameObject) {
			return;
		}
	}
	
	void OnGUI ()
	{
		Vector3 pos = transform.position;
		/*
		pos.z = 1.0f;
		pos.y += 0.06f;
		lifeGauge.SetPoint(pos);
		pos.y -= 0.02f;
		moveGauge.SetPoint(pos);
		lifeGauge.SetScale(0.2f);
		moveGauge.SetScale(0.2f);
		pos.y -= 0.02f;
		attackGauge.SetPoint(pos);
		attackGauge.SetScale(0.2f);
		*/
		
		if (moving > 0) {
			texture.guiTexture.pixelInset = new Rect (-size / 2 - 32, -size / 2, size, size);
		} else {
			texture.guiTexture.pixelInset = new Rect (-size / 2, -size / 2, size, size);
		}
		
		if (Charas.instance.actChara == gameObject) {
			Rect rect = new Rect (
				texture.transform.position.x * Screen.width - 50,
				(1 - texture.transform.position.y) * Screen.height + 60 + 10.0f * (Mathf.Sin (Time.time * 10)), 100, 64);
			GUI.color = Color.blue;
			GUI.DrawTexture (rect, ButtonManager.cursorTexture);
			rect = new Rect (
				texture2.transform.position.x * Screen.width - 50,
				(1 - texture2.transform.position.y) * Screen.height + 120 + 10.0f * (Mathf.Sin (Time.time * 10)), 100, 64);
			GUI.DrawTexture (rect, ButtonManager.cursorTexture);
			GUI.color = Color.white;
		}
		DrawBuffGUI ();
		
		for (int i = 0; i < mana; ++i) {
			pos = GUIManager.GetPositionFromRelative (transform.position);
			Rect rect = new Rect (
				pos.x - 48 + i * 32,
				Screen.height - pos.y - 148,
				32, 32
				);
			GUI.DrawTexture (rect, ButtonManager.buffTextures [(int)EnumBuff.MANA]);
		}
		
		if (Charas.instance.showReserve) {
			Charas.instance.ShowReserveGUI ();
		}
	}
	
	virtual public void DrawBuffGUI ()
	{
		int i = 0;
		foreach (EnumBuff buff in buffs.Keys) {
			Vector3 pos = GUIManager.GetPositionFromRelative (transform.position);
			Rect rect = new Rect (
				pos.x - size / 2.0f,
				Screen.height - pos.y - size - 20 + i * 32,
				32,
				32
				);
			if(buffs[buff].time > 2.0f || (int)(buffs[buff].time * 8) % 2 == 0){
				GUI.DrawTexture (rect, ButtonManager.buffTextures [(int)buff]);
			}
			++i;
		}
	}
	
	void DoNothing (object obj)
	{
		
	}
	
	void ShowBench (object obj)
	{
		if (Charas.instance.reserveCharas.Count > 0) {
			Charas.instance.showReserve = true;
		}
	}
	
	void IntoBench (object obj)
	{
		Charas.instance.Swap (gameObject);
	}
	
	protected void ChangeFormation (object obj)
	{
		Charas.instance.Move (gameObject);
		CreatePanels(EnumPanelState.DEFAULT);
	}
	
	protected void HealSelf (object obj)
	{
		int magic = param.magic * 3;
		if (HasBuff (EnumBuff.TROUBADOUR)) {
			magic *= 2;
		}
		if (param.isEnemy) {
			if (GameObject.Find ("Boss(Clone)")) {
				GameObject.Find ("Boss(Clone)").GetComponent<Chara> ().Heal (magic);
			} else {
				Heal (magic);
			}
			Moving (1.0f);
			return;
		}
		if (Charas.instance.TargetFriend) {
			Charas.instance.TargetFriend.GetComponent<Chara> ().Heal (magic);
		} else {
			Heal (magic);
		}
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [2]);
		Moving (1.0f);
	}
	
	protected void Provoke (object obj)
	{
		StrAttack attack = new StrAttack ();
		attack.pow = 0;
		attack.buff = EnumBuff.PROVOKE;
		attack.buffParam.time = 10.0f * (1 + mana);
		Damage (attack);
		Moving (1.0f);
	}
	
	protected void Slash (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			Moving (1.0f);
			StrAttack attack;
			SetAttackParam (out attack);
			for (int i = 0; i < mana + 1; ++i) {
				Vector3 vel = Vector2.zero;
				vel.x = -0.004f * i;
				Effect2D.Create (enemy.transform.position, vel, transform.position, 2.0f, 1, Effect2D.EnumEffect.EFF_SLASH, attack);
			}
		}
	}
	
	protected void Armor (object obj)
	{
		AddBuff (EnumBuff.ARMOR, 5.0f);
	}
	
	protected void PoisonSlash (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			Moving (1.0f);
			StrAttack attack;
			SetAttackParam (out attack);
			attack.buff = EnumBuff.POISON;
			attack.pow = (int)((float)attack.pow * 0.1f);
			attack.buffParam.time = 5.0f * (1 + mana);
			
			Effect2D.Create (enemy.transform.position, Vector2.zero, transform.position, 2.0f, 1, Effect2D.EnumEffect.EFF_POISON_SLASH, attack);
		}
	}
	
	virtual public GameObject GetTarget (GameObject from)
	{	
		GameObject[] enemyList = GameObject.FindGameObjectsWithTag ("Enemy");
		List<GameObject> enemies = new List<GameObject> ();
		foreach (GameObject enemy in enemyList) {
			if (enemy.GetComponent<Chara> () && enemy.GetComponent<Chara> ().Live ()) {
				enemies.Add (enemy);
			}
		}
		List<GameObject> t = enemies.FindAll (o => o.GetComponent<Chara> ().Live ());
		t.Sort (HateCompare);
		if (t.Count == 0) {
			return null;
		}
		return t [0];
	}
	
	protected void Meteor (object obj)
	{
		for (int i = 0; i < 10; ++i) {
			SetStack (GenerateMeteor, 0.5f * i);
		}
	}
	
	void GenerateMeteor (object obj)
	{
		GameObject[] enemies = GetAllTarget ();
		if (enemies.Length == 0) {
			return;
		}
		GameObject enemy = enemies [Random.Range (0, enemies.Length)];
		if (enemy) {
			Moving (1.0f);
			Vector2 targetPos = enemy.transform.position;
			Vector2 generatePos = targetPos;
			generatePos.y += 0.3f;
			
			StrAttack attack;
			SetAttackParam (out attack);
			attack.whom = enemy;
			attack.pow = (int)(attack.pow * 0.3f * (1 + mana));
			
			Effect2D.Create (targetPos, Vector2.zero, generatePos, 2.0f, 1, Effect2D.EnumEffect.EFF_CLASH, attack);
		}
	}
	
	protected void Clash (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			Moving (1.0f);
			Vector2 targetPos = enemy.transform.position;
			Vector2 generatePos = targetPos;
			
			StrAttack attack;
			SetAttackParam (out attack);
			attack.buff = EnumBuff.CLASH;
			
			for (int i = 0; i < mana + 1; ++i) {
				generatePos.y += 0.3f;
				Effect2D.Create (targetPos, Vector2.zero, generatePos, 2.0f, 1, Effect2D.EnumEffect.EFF_CLASH, attack);
			}
		}
	}
	
	protected void Fire (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		GameObject[] enemies = GetAllTarget ();
		if (enemy) {
			int targetRow = Charas.instance.GetCharaRow (enemy);
			Moving (1.0f);
			
			StrAttack attack;
			SetAttackParam (out attack);
			attack.pow += param.magic;
			//attack.buff = EnumBuff.FIRE;
			//attack.buffParam.time = 3.0f * (1 + mana);
			//attack.buffParam.param = 0.01f / (1 + mana);
			
			foreach (GameObject target in enemies) {
				if (Charas.instance.GetCharaRow (target) == targetRow) {
					attack.whom = target;
					Vector2 targetPos = target.transform.position;
					Effect2D.Create (targetPos, Vector2.zero, transform.position, 2.0f, 1, Effect2D.EnumEffect.EFF_FIRE, attack);
				}
			}
		}
	}
	
	protected void UltimateFire (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		GameObject[] enemies = GetAllTarget ();
		if (enemy) {
			int targetRow = Charas.instance.GetCharaRow (enemy);
			Moving (1.0f);
			
			StrAttack attack;
			SetAttackParam (out attack);
			attack.pow = param.attack * 3;
			attack.buff = EnumBuff.FIRE;
			attack.buffParam.time = 3.0f * (1 + mana);
			attack.buffParam.param = 0.01f / (1 + mana);
			
			foreach (GameObject target in enemies) {
				if (Charas.instance.GetCharaRow (target) == targetRow) {
					attack.whom = target;
					Vector2 targetPos = target.transform.position;
					Effect2D.Create (targetPos, Vector2.zero, transform.position, 2.0f, 1, Effect2D.EnumEffect.EFF_FIRE, attack);
				}
			}
		}
	}
	
	protected void Freeze (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			StrAttack attack;
			SetAttackParam (out attack);
			attack.buff = EnumBuff.FREEZE;
			attack.buffParam.time = 5.0f + (mana * 5.0f);
			attack.pow = 0;
			enemy.GetComponent<Chara> ().Damage (attack);
			GameObject.Find ("Sound").audio.PlayOneShot (sounds [6]);
		}
	}
	
	protected void Heal (object obj)
	{
		life += (int)obj;
		if (life > param.life) {
			life = param.life;
		}
		Debug.Log ((int)obj + "回復");
		text.guiText.text = life + "/" + param.life;
		lifeGauge.SetNowValue (life);
	}
	
	virtual protected void AddHate (GameObject whose, int dmg)
	{
		
	}
	
	virtual public int Damage (StrAttack attack)
	{
		int dmg = attack.pow;
		dmg = (int)((Random.Range (dmg * 0.4f, dmg * 0.6f) + Random.Range (dmg * 0.4f, dmg * 0.6f)) / 2.0f);
		dmg = (int)((float)dmg * GetRatio (attack.attr, param.attr));
		if (HasBuff (EnumBuff.ARMOR)) {
			//dmg -= (50 * (1 + mana));
			dmg /= (2 + mana);
		}
		if (attack.buff == EnumBuff.CLASH) {
			CureBuff (EnumBuff.REFLECT);
		}
		if (attack.buff == EnumBuff.FIRE) {
			CureBuff (EnumBuff.FREEZE);
		}
		if (HasBuff (EnumBuff.FREEZE)) {
			if (attack.buff == EnumBuff.CLASH) {
				CureBuff (EnumBuff.FREEZE);
				dmg *= 5;
			} else {
				dmg = 0;
			}
		}
		if (HasBuff (EnumBuff.GENERAL)) {
			dmg = (int)(dmg * 0.7f);
		}
		if (HasBuff (EnumBuff.PROTECTION)) {
			StrBuff tempBuff = buffs [EnumBuff.PROTECTION];
			tempBuff.param = (int)tempBuff.param - (int)dmg;
			if ((int)tempBuff.param <= 0) {
				CureBuff (EnumBuff.PROTECTION);
				dmg = -(int)tempBuff.param;
			} else {
				dmg = 0;
				buffs [EnumBuff.PROTECTION] = tempBuff;
				Debug.Log ((int)tempBuff.param);
			}
		}
		if (HasBuff (EnumBuff.SERENADE)) {
			Debug.Log ("セレナーデ" + dmg + "->" + (dmg - GetBuffParam<int>(EnumBuff.SERENADE)));
			dmg -= GetBuffParam<int>(EnumBuff.SERENADE);
		}
		if (dmg < 0) {
			dmg = 0;
		}
		else{
			++hitCount;
		}
		life -= (int)dmg;
		TextManager.ShowText ("" + (int)dmg, 0.5f, new Vector2 (0.5f, 0.5f), Color.red);
		AddHate (attack.whose, (int)dmg);
		if (param.isEnemy) {
			++Charas.instance.Combo;
		}
		if (HasBuff (EnumBuff.REFLECT) && attack.whose) {
			StrAttack reflect = attack;
			reflect.pow = dmg * (2 + (mana));
			reflect.whose = null;
			attack.whose.GetComponent<Chara> ().Damage (reflect);
		}
		
		if (life <= 0 && life + (int)dmg > 0) {
			if (attack.whose) {
				attack.whose.GetComponent<Chara> ().Mana = attack.whose.GetComponent<Chara> ().Mana + 1;
				attack.whose.GetComponent<Chara> ().param.strength += 3;
				attack.whose.GetComponent<Chara> ().param.agility += 3;
				attack.whose.GetComponent<Chara> ().param.intelligence += 3;
				GameObject.Find ("Sound").audio.PlayOneShot (sounds [6]);
			} else {
			}
			//chara.renderer.material.SetColor("_Color", Color.grey);
			//animation.Stop();
			//particleSystem.enableEmission = false;
			life = 0;
			gameObject.SetActive (false);
			if(menu){
				menu.Clear();
			}
			Dead ();
			Charas.instance.Dead ();
		} else {
			//animation.Play("damage", PlayMode.StopAll);
			//animation.PlayQueued("chara");
		}
		text.guiText.text = life + "/" + param.life;
		if (attack.buffParam.time > 0) {
			if (attack.buff == EnumBuff.POISON) {
				attack.buffParam.param = attack.pow;
			}
			AddBuff (attack.buff, attack.buffParam);
		}
		lifeGauge.SetNowValue (life);
		return dmg;
	}
	
	virtual public void Dead ()
	{
	}
	
	public void AddBuffAll (EnumBuff buff, float time)
	{
		GameObject[] charas;
		if(!param.isEnemy){
			charas = Charas.instance.GetAllCharas ();
		}
		else{
			charas = Charas.instance.GetAllEnemies ();
		}
		
		foreach (GameObject chara in charas) {
			chara.GetComponent<Chara> ().AddBuff (buff, time);
		}
	}
	
	public void AddBuffAll (EnumBuff buff, StrBuff buffParam)
	{
		GameObject[] charas;
		if(!param.isEnemy){
			charas = Charas.instance.GetAllCharas ();
		}
		else{
			charas = Charas.instance.GetAllEnemies ();
		}
		foreach (GameObject chara in charas) {
			chara.GetComponent<Chara> ().AddBuff (buff, buffParam);
		}
	}
	
	public void AddBuffAllEnemy (EnumBuff buff, float time)
	{
		GameObject[] charas;
		if(!param.isEnemy){
			charas = Charas.instance.GetAllEnemies ();
		}
		else{
			charas = Charas.instance.GetAllCharas ();
		}
		foreach (GameObject chara in charas) {
			chara.GetComponent<Chara> ().AddBuff (buff, time);
		}
	}
	
	public void AddBuff (EnumBuff buff, float time)
	{
		StrBuff buffParam = new StrBuff ();
		buffParam.time = time;
		if (HasBuff (buff)) {
			buffs [buff] = buffParam;
		} else {
			buffs.Add (buff, buffParam);
		}
	}
	
	public void AddBuff (EnumBuff buff, StrBuff buffParam)
	{
		if (HasBuff (buff)) {
			buffs [buff] = buffParam;
		} else {
			buffs.Add (buff, buffParam);
		}
	}
	
	public void CureBuff ()
	{
		buffs.Clear ();
	}
	
	public void CureBuff (EnumBuff buff)
	{
		buffs.Remove (buff);
		if(buff == EnumBuff.HIDE){
			texture.guiTexture.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
	}
	
	public void SetAttackParam (out StrAttack pOut)
	{
		pOut.attr = param.attr;
		pOut.pow = param.attack;
		pOut.whom = GetTarget (gameObject);
		pOut.whose = gameObject;
		pOut.targetDiv = StrAttack.TargetDiv.DIRECT;
		pOut.buff = 0;
		pOut.buffParam.time = 0;
		pOut.buffParam.param = null;
	}
	
	public static float GetRatio (Attribute attack, Attribute defense)
	{
		return 1.0f;
	}
	
	public void SetActionIcons (GameObject[] icons)
	{
		icons [0].guiTexture.texture = ButtonManager.GetIconTexture (actionList [param.actionID [0]].actionID);
		icons [1].guiTexture.texture = Resources.Load <Texture> ("Chara/CHO/Icon/Boots");
		icons [2].guiTexture.texture = ButtonManager.GetIconTexture (actionList [param.actionID [2]].actionID);
		icons [3].guiTexture.texture = ButtonManager.GetIconTexture (actionList [param.actionID [3]].actionID);
		icons [0].transform.position = new Vector3 (transform.position.x, transform.position.y + 0.12f, 3);
		icons [1].transform.position = new Vector3 (transform.position.x + 0.16f, transform.position.y, 3);
		icons [2].transform.position = new Vector3 (transform.position.x, transform.position.y - 0.12f, 3);
		icons [3].transform.position = new Vector3 (transform.position.x - 0.16f, transform.position.y, 3);
	}
	
	public bool Live ()
	{
		return life > 0;
	}
	
	public bool HasBuff (EnumBuff checkBuff)
	{
		return buffs.ContainsKey (checkBuff);
	}
	
	public T GetBuffParam<T> (EnumBuff checkBuff)
	{
		return (T)buffs [checkBuff].param;
	}
	
	virtual public void MoveStart (object obj)
	{
		if (obj != null) {
			if(movePoint >= (int)obj){
				movePoint -= (int)obj;
				moveGauge.SetColor(Color.blue);
			}
		}
		Charas.instance.Stop(false);
		waiting = false;
	}
	
	virtual public void MoveEnd (object obj)
	{
		moveGauge.SetColor (Color.blue);
		if (HasBuff (EnumBuff.POISON)) {
			StrAttack poison = new StrAttack ();
			poison.pow = (int)buffs [EnumBuff.POISON].param;
			poison.whom = gameObject;
			poison.whose = null;
			poison.targetDiv = StrAttack.TargetDiv.DIRECT;
			GameObject.Find ("Sound").audio.PlayOneShot(sounds[7]);
			Damage (poison);
		}
		if (button) {
			button.animation.Stop ();
		}
		if (!Charas.instance.showReserve) {
			//Charas.instance.actChara = null;
		}
		//CureBuff (EnumBuff.HIDE);
		Charas.instance.actChara = null;
	}
	
	virtual public int HateValue ()
	{
		int hate = 0;
		//?????????????D??0?`100
		hate += (int)((float)(param.life - life) / param.life * 100.0f);
		if (HasBuff (EnumBuff.KING)) {
			hate += 100;
		}
		if (HasBuff (EnumBuff.THIEF)) {
			hate -= 100;
		}
		if (HasBuff (EnumBuff.HIDE)) {
			hate -= 50;
		}
		//????100
		if (HasBuff (EnumBuff.PROVOKE)) {
			hate += 100 * (1 + mana);
		}

		hate -= 50 * row;
		//?_???[?W???????^????????
		if (targetFrom.GetComponent<Enemy> ().hateList.ContainsKey (gameObject)) {
			hate += (int)((float)targetFrom.GetComponent<Enemy> ().hateList [gameObject] / param.life * 300.0f);
		}
		return hate;
	}
	
	public static int HateCompare (GameObject a, GameObject b)
	{
		return b.GetComponent<Chara> ().HateValue () - a.GetComponent<Chara> ().HateValue ();
	}
	
	public void CreatePanels (object obj){
		if (param.isEnemy) {
			return;
		}
		if(obj != null){
			panelState = (EnumPanelState)obj;
		}
		int[] panels;

		switch(panelState){
		case EnumPanelState.SKILL:
			panels = new int[param.actionID.Length];
			panels = param.actionID;
			break;
		default:
		case EnumPanelState.DEFAULT:
			panels = new int[]{
				(int)EnumAction.COMMON_SKILL,
				(int)EnumAction.COMMON_DEFENSE,
				(int)EnumAction.COMMON_ITEM,
				(int)EnumAction.COMMON_ULTIMATE,
				(int)EnumAction.COMMON_FORMATION,
				(int)EnumAction.COMMON_WAIT,
			};
			break;
		case EnumPanelState.ITEM:
			panels = new int[1];
			panels[0] = (int)EnumAction.COMMON_SKILL_CLOSE;
			break;
		}
		menu.Clear ();
		if (Charas.instance.actChara == gameObject) {
			for (int i = 0; i < panels.Length; ++i) {
				CreatePanel (panels[i]);
			}
		}
	}
	
	void CreatePanel (int actionID){
		delStackFunction func;
		if(actionList [actionID].cost > 0){
			func = MoveStart + actionList [actionID].func + MoveEnd + CreatePanels;
		}
		else{
			func =  actionList [actionID].func;
		}
		UtilPanel newPanel = UtilPanel.Create ();
		newPanel.SetText (actionList [actionID].name);
		//Debug.Log (actionList [actionID].name);
		newPanel.SetSubText (actionList [actionID].detail);
		newPanel.SetFunction (func, actionList [actionID].cost);
		if (HasBuff(EnumBuff.TIRED) || actionList [actionID].cost > movePoint ||
		    (actionID == (int)EnumAction.COMMON_BENCH && Charas.instance.reserveCharas.Count == 0)
		    || (actionID == (int)EnumAction.COMMON_ULTIMATE && Charas.instance.Combo < Ult.ULT_COMBO)) {
			newPanel.guiTexture.color = Color.Lerp (Color.black, Color.gray, 0.5f);
			//delStackFunction temp = DoNothing;
			//delStackFunction f = MoveStart + temp + MoveEnd;
			newPanel.SetFunction (DoNothing, actionList [actionID].cost);
		}else{
			if(actionID == (int)EnumAction.COMMON_ULTIMATE){
				if(ult){
					newPanel.guiTexture.color = Color.Lerp (Color.red, Color.white, 0.2f);
				}
			}
		}
		
		//newPanel.transform.parent = children.transform;
		menu.AddMenu (newPanel);
	}
	
	protected void Phantom (object obj)
	{
		GameObject newChara = null;
		newChara = Instantiate (Charas.instance.charaModel) as GameObject;
		Charas.instance.charas.Add (newChara);
		
		if (newChara) {
			Charas.instance.SetChara ();
			StrParam newParam = param;
			newParam.agility = param.agility * (1 + mana);
			newChara.GetComponent<Chara> ().Init (newParam, 
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[0].ActionID,
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[1].ActionID,
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[2].ActionID);
			newChara.GetComponent<Chara> ().AddBuff (EnumBuff.PHANTOM, 5.0f * (1 + mana));
			
			GameObject.Find ("Sound").audio.PlayOneShot(sounds[3]);
		}
	}
	
	protected void Summon (object obj)
	{
		GameObject newChara = null;
		newChara = Instantiate (Charas.instance.charaModel) as GameObject;
		Charas.instance.charas.Add (newChara);
		if (newChara) {
			Charas.instance.SetChara ();
			StrParam newParam = param;
			newParam.charaID = 9;
			newParam.agility = param.agility * (1 + mana);
			newChara.GetComponent<Chara> ().Init (newParam, 
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[0].ActionID,
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[1].ActionID,
			                                      Charas.instance.jobAction.sheets[newParam.charaID].list[2].ActionID);
			newChara.GetComponent<Chara> ().AddBuff (EnumBuff.PHANTOM, 5.0f * (1 + mana));
			
			GameObject.Find ("Sound").audio.PlayOneShot(sounds[8]);
		}
	}
	
	protected void Paralysis (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			Moving (1.0f);
			StrAttack attack;
			SetAttackParam (out attack);
			attack.buff = EnumBuff.PARALYSIS;
			attack.buffParam.time = 10.0f * (1 + mana);
			Effect2D.Create (enemy.transform.position, Vector2.zero, transform.position, 2.0f, 1, Effect2D.EnumEffect.EFF_PARALYSIS, attack);
		}
	}
	
	protected void Bomb (object obj)
	{
		AddBuff (EnumBuff.BOMB, 3.0f);
	}
	
	protected virtual GameObject[] GetAllTarget ()
	{
		return Charas.instance.GetAllEnemies ();
	}
	
	protected void Bomber (object obj)
	{
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [1]);
		GameObject[] all = GetAllTarget ();
		StrAttack attack;
		SetAttackParam (out attack);
		attack.pow = life / (2 * (1 + mana));
		Damage (attack);
		attack.pow = (int)(attack.pow * (1 + mana));
		foreach (GameObject enemy in all) {
			enemy.GetComponent<Chara> ().Damage (attack);
		}
	}
	
	protected void Ultimate (object obj)
	{
		if(Charas.instance.Combo < Ult.ULT_COMBO){
			//return;
		}
		int count = mana + 1;
		if (ult) {
			count = Charas.instance.Combo;
			if (count > 50) {
				count = 50;
			}
			Ult.comboTime = 5.0f;
		}
		StartCoroutine("CreateClash", count);
		AddBuff (EnumBuff.TIRED, 300.0f);
	}
	
	IEnumerator CreateClash(int count){
		StrAttack attack;
		SetAttackParam (out attack);
		attack.buff = EnumBuff.CLASH;
		for (int i = 0; i < count; ++i){
			GameObject enemy = GetTarget (gameObject);
			if (enemy) {attack.whom = enemy;
				Moving (1.0f);
				Vector2 targetPos = enemy.transform.position;
				targetPos.y += 0.05f;
				Effect2D.Create (targetPos, Vector2.zero, transform.position, 1.0f, 1, Effect2D.EnumEffect.EFF_CLASH, attack);
				yield return new WaitForSeconds(0.1f);
			}
		}
	}
	
	protected void Fly (object obj)
	{
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [3]);
		AddBuff (EnumBuff.FLYING, 5.0f);
		//AddBuffAll(EnumBuff.FLYING, 5.0f);
	}
	
	protected void Reflect (object obj)
	{
		AddBuff (EnumBuff.REFLECT, 5.0f * (1 + mana));
	}
	
	protected void Hide (object obj)
	{
		AddBuff (EnumBuff.HIDE, 5.0f * (1 + mana));
		texture.guiTexture.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
	}
	
	protected void Drain (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			Moving (1.0f);
			StrAttack attack;
			SetAttackParam (out attack);
			attack.pow *= 2;
			int dmg = enemy.GetComponent<Chara> ().Damage (attack);
			Heal ((int)(dmg));
		}
	}
	
	virtual protected void NormalAttack (object obj)
	{
		if (param.weaponType != EnumWeaponType.GUN && Charas.instance.GetCharaRow (gameObject) > 0) {
			return;
		}
		if(param.isEnemy && hitCount >= 20){
			hitCount = 0;
			UltimateFire (null);
			return;
		}
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			float hitRate = GetWeaponHitRate ();
			if (enemy.GetComponent<Chara> ().HasBuff (EnumBuff.HAWK_EYE)) {
				hitRate = 1.0f;
			}
			if (Random.Range (0, 1.0f) > hitRate) {
				Moving (1.0f);
				//Debug.Log ("?????????[?I");
				GameObject.Find ("Sound").audio.PlayOneShot (sounds [5]);
				return;
			}
			bool crt = false;
			Moving (1.0f);
			StrAttack attack;
			SetAttackParam (out attack);
			attack.pow = (int)(attack.pow * GetWeaponWait () * 0.1f);
			//Debug.Log("pow" + attack.pow);
			if (HasBuff (EnumBuff.ARCHER) && Charas.instance.GetCharaRow (enemy) > 0) {
				attack.pow *= 2;
			}
			if (HasBuff (EnumBuff.PALADIN) && Charas.instance.GetCharaRow (enemy) == 0) {
				attack.pow *= 2;
			}
			Vector3 pos = enemy.transform.position;
			float crtRate = GetCriticalRate ();
			if (enemy.GetComponent<Chara> ().HasBuff (EnumBuff.HAWK_EYE)) {
				crtRate *= 1.5f;
			}
			if(enemy.GetComponent<Chara> ().HasBuff(EnumBuff.BARD)){
				crtRate = 0;
			}
			if(HasBuff(EnumBuff.BOLERO)){
				attack.pow += GetBuffParam<int>(EnumBuff.BOLERO);
			}
			if (Random.Range (0, 1.0f) < crtRate) {
				attack.pow *= 3;
				if (HasBuff (EnumBuff.FIGHTER)) {
					attack.pow *= 2;
				}
				crt = true;
			}
			pos.y += 0.1f;
			if (param.weaponType == EnumWeaponType.GUN) {
				if (crt) {
					Effect2D.Create (pos, Vector2.zero, pos, 0.6f, 1, Effect2D.EnumEffect.EFF_CRT_GUN, attack);
				} else {
					Effect2D.Create (pos, Vector2.zero, pos, 0.6f, 1, Effect2D.EnumEffect.EFF_GUN, attack);
				}
			} else {
				Vector3 target = pos;
				target.y -= 0.02f;
				target.x -= 0.02f;
				if (crt) {
					Effect2D.Create (target, Vector2.zero, pos, 0.6f, 1, Effect2D.EnumEffect.EFF_CRT_SLASH, attack);
				} else {
					Effect2D.Create (target, Vector2.zero, pos, 0.6f, 1, Effect2D.EnumEffect.EFF_SLASH, attack);
				}
			}
			if (HasBuff (EnumBuff.BLOOD)) {
				Heal ((int)(attack.pow * 0.2f));
			}
		}
	}
	
	protected void Ax (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			if (Charas.instance.GetCharaRow (enemy) == 0) {
				GameObject.Find ("Sound").audio.PlayOneShot (sounds [1]);
				Moving (1.0f);
				StrAttack attack;
				SetAttackParam (out attack);
				attack.buff = EnumBuff.STUN;
				attack.buffParam.time = 2.0f * (1 + mana);
				int dmg = enemy.GetComponent<Chara> ().Damage (attack);
			}
		}
	}
	
	protected void Protection (object obj)
	{
		StrBuff protection;
		protection.param = 300;
		protection.time = 30.0f;
		if (Charas.instance.TargetFriend) {
			Charas.instance.TargetFriend.GetComponent<Chara> ().AddBuff (EnumBuff.PROTECTION, protection);
		} else {
			AddBuff (EnumBuff.PROTECTION, protection);
		}
		Moving (1.0f);
	}
	
	protected void HawkEye (object obj)
	{
		GameObject enemy = GetTarget (gameObject);
		if (enemy) {
			enemy.GetComponent<Chara> ().AddBuff (EnumBuff.HAWK_EYE, 20.0f);
		}
		Moving (1.0f);
	}
	
	protected void Regret (object obj){
		StrBuff buff = new StrBuff();
		buff.time = 20.0f;
		buff.param = 1.2f + (0.2f * mana);
		AddBuffAll(EnumBuff.REGRET, buff);
		Moving (1.0f);
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [3]);
	}
	protected void Bolero (object obj){
		StrBuff buff = new StrBuff();
		buff.time = 20.0f;
		buff.param = 20 + mana * 5;
		AddBuffAll(EnumBuff.BOLERO, buff);
		Moving (1.0f);
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [3]);
	}
	protected void Requiem (object obj){
		StrBuff buffParam = new StrBuff();
		buffParam.param = 20.0f;
		buffParam.time = 20.0f;
		AddBuffAll(EnumBuff.REQUIEM, buffParam);
		Moving (1.0f);
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [3]);
	}
	protected void Serenade (object obj){
		StrBuff buff = new StrBuff();
		buff.time = 20.0f;
		buff.param = 5 + mana * 2;
		AddBuffAll(EnumBuff.SERENADE, buff);
		Moving (1.0f);
		GameObject.Find ("Sound").audio.PlayOneShot (sounds [3]);
	}
	
	protected void Skill (object obj)
	{
		CreatePanels (EnumPanelState.SKILL);
	}
	
	protected void Item (object obj)
	{
		CreatePanels (EnumPanelState.ITEM);
	}
	
	protected void SkillClose (object obj)
	{
		CreatePanels(EnumPanelState.DEFAULT);
	}
	
	protected void Wait (object obj)
	{
		waiting = true;
		Charas.instance.Stop (false);
	}
	
	public void ActionInit (int a, int b, int c)
	{
		actionList = new StrAction[(int)EnumAction.MAX];
		
		actionList [(int)EnumAction.AX] = new StrAction (73, "メガアックス", "前列の対象を3秒間行動不能にする", 
		                                                 Ax, 500, false);
		actionList [(int)EnumAction.BOMB] = new StrAction (13, "自爆", "自身の命と引き換えに全体攻撃", 
		                                                   Bomb, 500, false);
		actionList [(int)EnumAction.DRAIN] = new StrAction (171, "吸血", "与えたダメージ分を回復", 
		                                                    Drain, 500, false);
		
		actionList [(int)EnumAction.PROVOKE] = new StrAction (163, "挑発", "しばらく攻撃を引き付ける", 
		                                                      Provoke, 500, false);
		actionList [(int)EnumAction.HEAL_SELF] = new StrAction (61, "回復", "体力を回復", 
		                                                        HealSelf, 500, false);
		actionList [(int)EnumAction.ARMOR] = new StrAction (62, "鉄壁", "5秒間ダメージを半減", 
		                                                    Armor, 500, false);
		
		actionList [(int)EnumAction.POSION_SLASH] = new StrAction (188, "侵食毒", "行動のたびにダメージを与える", 
		                                                           PoisonSlash, 500, false);
		actionList [(int)EnumAction.METEOR] = new StrAction (144, "流星群", "ランダムで隕石10個落とす", 
		                                                     Meteor, 500, false);
		actionList [(int)EnumAction.PARALYSIS] = new StrAction (181, "麻痺毒", "しばらくスピードを半減させる", 
		                                                        Paralysis, 500, false);
		
		actionList [(int)EnumAction.FLY] = new StrAction (191, "飛行", "5秒間、行動速度が二倍", 
		                                                  Fly, 500, false);
		actionList [(int)EnumAction.SLASH] = new StrAction (25, "裂空斬", "単体を斬撃で攻撃", 
		                                                    Slash, 500, false);
		actionList [(int)EnumAction.REFLECT] = new StrAction (35, "反射", "5秒間、受けたダメージの二倍を相手にも与える", 
		                                                      Reflect, 500, false);
		actionList [(int)EnumAction.BLOOD] = new StrAction (35, "ムラマサ", "通常攻撃で与えたダメージの20%を回復", 
		                                                    Wolf, 2000, true);
		
		actionList [(int)EnumAction.HIDE] = new StrAction (166, "暗殺術", "5秒間身を隠し、クリティカル発生率3倍", 
		                                                   Hide, 500, false);
		actionList [(int)EnumAction.FREEZE] = new StrAction (228, "氷封", "凍らせて行動不能かつ無敵にする", 
		                                                     Freeze, 500, false);
		actionList [(int)EnumAction.CLASH] = new StrAction (205, "岩石落とし", "反射を解除し氷漬け状態の相手に大ダメージ", 
		                                                    Clash, 500, false);
		actionList [(int)EnumAction.FIRE] = new StrAction (205, "フレイムベイン", "横一列に炎ダメージ", 
		                                                   Fire, 500, false);
		actionList [(int)EnumAction.PROTECTION] = new StrAction (205, "プロテクション", "300ダメージを無効化するシールドを張る", 
		                                                         Protection, 500, false);
		actionList [(int)EnumAction.HAWK_EYE] = new StrAction (171, "鷹の目", "対象への攻撃が外れなくなり、必殺率1.5倍、挑発も無視", 
		                                                       HawkEye, 500, false);
		actionList [(int)EnumAction.PHANTOM] = new StrAction (158, "分身術", "分身を生成する", 
		                                                      Phantom, 500, false);
		actionList [(int)EnumAction.SUMMON] = new StrAction (158, "召喚術", "使い魔を召喚する", 
		                                                     Summon, 500, false);
		actionList [(int)EnumAction.SERENADE] = new StrAction (158, "水のセレナーデ", "味方の受けるダメージを5減らす",
		                                                       Serenade, 500, false);
		actionList [(int)EnumAction.BOLERO] = new StrAction (158, "炎のボレロ", "味方の与えるダメージを20増やす", 
		                                                     Bolero, 500, false);
		actionList [(int)EnumAction.REQUIEM] = new StrAction (158, "魂のレクイエム", "味方全体を徐々に回復", 
		                                                      Requiem, 500, false);
		actionList [(int)EnumAction.REGRET] = new StrAction (158, "風のリグレット", "味方全体の攻撃速度を増加", 
		                                                     Regret, 500, false);
		actionList [(int)EnumAction.ARCHER] = new StrAction (171, "アーチャー", "後衛へのダメージが2倍", 
		                                                     Archer, 2000, true);
		actionList [(int)EnumAction.FIGHTER] = new StrAction (171, "ファイター", "クリティカルダメージが2倍", 
		                                                      Fighter, 2000, true);
		actionList [(int)EnumAction.GENERAL] = new StrAction (171, "ジェネラル", "受けるダメージが30%減", 
		                                                      General, 2000, true);
		actionList [(int)EnumAction.KING] = new StrAction (171, "ベルン王", "敵に狙われやすい", 
		                                                   King, 2000, true);
		actionList [(int)EnumAction.MAGE] = new StrAction (171, "メイジ", "魔法ゲージ上昇スピードが1.5倍", 
		                                                   Mage, 2000, true);
		actionList [(int)EnumAction.PALADIN] = new StrAction (171, "パラディン", "前衛へのダメージが2倍", 
		                                                      Paladin, 2000, true);
		actionList [(int)EnumAction.THIEF] = new StrAction (171, "シーフ", "敵に狙われにくい", 
		                                                    Thief, 2000, true);
		actionList [(int)EnumAction.TROUBADOUR] = new StrAction (171, "トルバドール", "回復スキルの効果が2倍", 
		                                                         Troubadour, 2000, true);
		actionList [(int)EnumAction.BARD] = new StrAction (171, "バード", "クリティカルを受けない", 
		                                                   Bard, 2000, true);
		actionList [(int)EnumAction.SUMMONER] = new StrAction (171, "ピカチュウ", "でんきねずみポケモン", 
		                                                       null, 2000, true);
		
		actionList [(int)EnumAction.COMMON_ATTACK] = new StrAction (166, "攻撃", "", 
		                                                            NormalAttack, 500, false);
		actionList [(int)EnumAction.COMMON_DEFENSE] = new StrAction (205, "防御", "", 
		                                                             Armor, 0, false);
		
		actionList [(int)EnumAction.COMMON_BENCH] = new StrAction (166, "交代", "", 
		                                                           ShowBench, 0, false);
		actionList [(int)EnumAction.COMMON_FORMATION] = new StrAction (205, "移動", "", 
		                                                               ChangeFormation, 0, false);
		actionList [(int)EnumAction.COMMON_ULTIMATE] = new StrAction (205, "最終奥義", "コンボが20以上で発動可", 
		                                                              Ultimate, 1000, false);
		actionList [(int)EnumAction.COMMON_SKILL] = new StrAction (191, "スキル", "スキルを使用する", 
		                                                           Skill, 0, false);
		actionList [(int)EnumAction.COMMON_SKILL_CLOSE] = new StrAction (191, "スキル", "スキルを使用しない", 
		                                                                 SkillClose, 0, false);
		actionList [(int)EnumAction.COMMON_ITEM] = new StrAction (191, "道具", "道具を使用する", 
		                                                          Item, 0, false);
		actionList [(int)EnumAction.COMMON_WAIT] = new StrAction (191, "待機", "", 
		                                                          Wait, 0, false);
		
		if (!param.isEnemy) {
			param.actionID = new int[4];
			//Debug.Log (a);
			param.actionID [0] = (int)EnumAction.COMMON_SKILL_CLOSE;
			param.actionID [1] = a;
			param.actionID [2] = b;
			param.actionID [3] = c;
		} else {
			param.actionID = new int[3];
			param.actionID [0] = Charas.instance.JobActionList (param.charaID) [a].ActionID;
			param.actionID [1] = Charas.instance.JobActionList (param.charaID) [b].ActionID;
			param.actionID [2] = Charas.instance.JobActionList (param.charaID) [c].ActionID;
		}
	}
	
	public void Archer(object obj){			AddBuff(EnumBuff.ARCHER, 1000.0f);		}
	public void Fighter(object obj){		AddBuff(EnumBuff.FIGHTER, 1000.0f);		}
	public void General(object obj){		AddBuff(EnumBuff.GENERAL, 1000.0f);		}
	public void King(object obj){			AddBuff(EnumBuff.KING, 		1000.0f);		}
	public void Mage(object obj){			AddBuff(EnumBuff.MAGE, 		1000.0f);		}
	public void Paladin(object obj){		AddBuff(EnumBuff.PALADIN, 	1000.0f);		}
	public void Thief(object obj){			AddBuff(EnumBuff.THIEF,		 1000.0f);		}
	public void Troubadour(object obj){		AddBuff(EnumBuff.TROUBADOUR, 1000.0f);		}
	public void Bard(object obj){			AddBuff(EnumBuff.BARD, 1000.0f);		}
	public void Wolf(object obj){			AddBuff(EnumBuff.BLOOD, 1000.0f);		}
	
	public bool HasSkill (EnumAction action)
	{
		foreach (EnumAction act in param.actionID) {
			if (act == action) {
				return true;
			}
		}
		return false;
	}
	
	public float GetNormalAttackWait ()
	{
		float wait = 1000.0f;
		wait *= GetWeaponWait ();
		return wait;
	}
	
	public float GetWeaponWait ()
	{
		float rate = 1.0f;
		switch (param.weaponType) {
		case EnumWeaponType.KNUCKLE:
			rate *= 0.6f;
			break;
		case EnumWeaponType.SHORT_SWORD:
			rate *= 0.8f;
			break;
		default:
		case EnumWeaponType.SWORD:
			rate *= 1.0f;
			break;
		case EnumWeaponType.GUN:
			rate *= 1.0f;
			break;
		case EnumWeaponType.SPEAR:
			rate *= 1.2f;
			break;
		case EnumWeaponType.AX:
			rate *= 1.4f;
			break;
		case EnumWeaponType.BOOK:
			rate *= 1.5f;
			break;
		}
		return rate;
	}
	
	public float GetWeaponHitRate ()
	{
		float rate = 1.0f;
		switch (param.weaponType) {
		case EnumWeaponType.KNUCKLE:
			rate *= 0.97f;
			break;
		case EnumWeaponType.SHORT_SWORD:
			rate *= 0.95f;
			break;
		default:
		case EnumWeaponType.SWORD:
			rate *= 0.93f;
			break;
		case EnumWeaponType.GUN:
			rate *= 0.84f;
			break;
		case EnumWeaponType.SPEAR:
			rate *= 0.90f;
			break;
		case EnumWeaponType.AX:
			rate *= 0.86f;
			break;
		case EnumWeaponType.BOOK:
			rate *= 0.80f;
			break;
		}
		return rate;
	}
	
	public float GetCriticalRate ()
	{
		float rate = 0.3f;
		if (HasBuff (EnumBuff.HIDE)) {
			rate *= 3.0f;
		}
		return rate;
	}
	
	public StrAction GetActionInfo (int num)
	{
		return actionList [num];
	}
	
	public bool LoadFromMemory (int id)
	{
		if (!PlayerPrefs.HasKey ("" + id)) {
			return false;
		}
		param = Save.Serialize.Load<Chara.StrParam> ("" + id);
		Init (param, param.actionID [0], param.actionID [1], param.actionID [2]);
		return true;
	}
}
