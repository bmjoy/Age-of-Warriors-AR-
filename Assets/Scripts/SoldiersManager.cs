﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoldiersManager : MonoBehaviour
{
    public enum S_STATE
    {
        S_NONE = 0,
        S_MOVING,
        S_WAITING,
        S_ATTACKING_BASE
    }

    private LayerMask enemy_layer;
    public Image hp_bar;
    public GameObject ally_base;
    public GameObject enemy_base;
    private Vector3 direction = new Vector3(0.0f, 0.0f, 0.0f);
    private S_STATE state;
    private GameObject target_fighting;

    public int gold_for_melee = 25;
    public int gold_for_ranged = 50;
    public int gold_for_cavalry = 150;

    [Header("Stats")]
    public PlayerBaseController.SOLDIER_TYPE type;
    public float hp = 100.0f;
    public float attack = 25.0f;
    public float attack_distance = 1.25f;
    public float waiting_distance = 1.0f;
    public float attack_speed = 1.0f;
    public float speed = 1.0f;

    private float max_hp = 100.0f;
    private float apply_dmg_timer = 0.0f;

    private bool fighting = false;
    private bool to_delete = false;

    public int enemy_layer_int = 8; // 8 is enemy layer

    private Animator anim;

    void Start()
    {
        direction = (enemy_base.transform.position - ally_base.transform.position);
        direction.Normalize();
        if (gameObject.layer == enemy_layer_int)
        {
            enemy_layer = LayerMask.NameToLayer("Ally");
        }
        else
        {
            enemy_layer = LayerMask.NameToLayer("Enemy");
        }
        state = S_STATE.S_MOVING;
        max_hp = hp;
        apply_dmg_timer = 0.0f;
        fighting = false;
        to_delete = false;

        anim = GetComponent<Animator>();
        anim.SetBool("walking", true);
    }

    void Update()
    {
		if (!to_delete) {
			Debug.DrawLine (transform.position, transform.position + direction.normalized * attack_distance, Color.yellow);
			if (!fighting) {
				RaycastHit target_hit;
				if (Physics.Raycast (transform.position, direction, out target_hit, attack_distance, (1 << enemy_layer))) {
					float d = Mathf.Abs (Vector3.Distance (ally_base.transform.position, enemy_base.transform.position));
					float enemy_hit_dist = Mathf.Abs (Vector3.Distance (target_hit.transform.position, transform.position));
					if (d > enemy_hit_dist) {
						d = enemy_hit_dist;
						fighting = true;
						target_fighting = target_hit.transform.gameObject;

						anim.SetBool ("walking", false);
						anim.SetBool ("attacking", true);

						Debug.Log ("New Target fighting: " + target_fighting.name);
					}
				}

				if (Mathf.Abs (Vector3.Distance (transform.position, enemy_base.transform.position)) <= attack_distance) {
					fighting = true;
					target_fighting = enemy_base;
					state = S_STATE.S_ATTACKING_BASE;

					anim.SetBool ("walking", false);
					anim.SetBool ("attacking", true);

					Debug.Log ("Attacking base");
				}
			}

			if (state == S_STATE.S_MOVING) {
				transform.position += direction * speed * Time.deltaTime;
				if (Physics.Raycast (transform.position, direction, waiting_distance)) {
					state = S_STATE.S_WAITING;
					anim.SetBool ("walking", false);
				}
			} else if (state == S_STATE.S_WAITING) {
				if (!Physics.Raycast (transform.position, direction, waiting_distance)) {
					state = S_STATE.S_MOVING;
					anim.SetBool ("walking", true);
				}
			}
			// if state is attacking base does nothing, he destroys the base or dies


			if (fighting) {
				apply_dmg_timer += Time.deltaTime;
				if (apply_dmg_timer >= (1.0f / attack_speed)) {
					apply_dmg_timer = 0.0f;
					if (target_fighting.name == "BaseAlly" || target_fighting.name == "BaseEnemy")
						target_fighting.GetComponent<BaseManager> ().ApplyDamage (attack);
					else {
						if (target_fighting.GetComponent<SoldiersManager> ().ApplyDamage (attack)) {
							fighting = false;
							target_fighting = null;
							anim.SetBool ("attacking", false);
						}
					}
				}
			}
		}
        if (to_delete)
        {
			switch(type)
			{
			case PlayerBaseController.SOLDIER_TYPE.S_MELEE:
				if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Footman_Death"))
					Destroy (gameObject);
				break;
			case PlayerBaseController.SOLDIER_TYPE.S_RANGED:
				if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Death1"))
					Destroy (gameObject);
				break;
			case PlayerBaseController.SOLDIER_TYPE.S_CAVALRY:
				if (anim.GetCurrentAnimatorStateInfo (0).IsName ("-"))
					Destroy (gameObject);
				break;
			case PlayerBaseController.SOLDIER_TYPE.S_NONE:
			default:
				Debug.Log ("Error: no type deleting soldier");
				break;
			}
		}
    }

    bool ApplyDamage(float dmg)
    {
        hp -= dmg;
        hp_bar.fillAmount = (hp / max_hp);
        if (hp <= 0)
        {
            if (gameObject.layer == enemy_layer_int)
            {
                switch (type)
                {
                    case PlayerBaseController.SOLDIER_TYPE.S_NONE:
                        break;
                    case PlayerBaseController.SOLDIER_TYPE.S_MELEE:
                        enemy_base.GetComponent<PlayerBaseController>().AddGold(gold_for_melee);
                        break;

                    case PlayerBaseController.SOLDIER_TYPE.S_RANGED:
                        enemy_base.GetComponent<PlayerBaseController>().AddGold(gold_for_ranged);
                        break;

                    case PlayerBaseController.SOLDIER_TYPE.S_CAVALRY:
                        enemy_base.GetComponent<PlayerBaseController>().AddGold(gold_for_cavalry);
                        break;

                    default:
                        break;
                }
            }
            anim.SetBool("die", true);
            GetComponent<CapsuleCollider>().enabled = false;
            to_delete = true;
            return true;
        }
        return false;
    }
}