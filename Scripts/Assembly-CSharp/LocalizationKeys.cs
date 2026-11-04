using System;
using CarProfile;
using DB.Meta;
using GameInput;
using SyncMultiplayer;
using UnityEngine;

public static class LocalizationKeys
{
	public class Key
	{
		protected string m_value;

		public string KeyValue => m_value;

		public Key(string s)
		{
			m_value = s;
		}

		public static implicit operator Key(string s)
		{
			return new Key(s);
		}

		public string Localized()
		{
			return LocalizationService.Localize(m_value);
		}

		public string LocalizedFormat(params object[] args)
		{
			return LocalizationService.LocalizeFormat(m_value, args);
		}

		public string FormatLocalized(params object[] args)
		{
			return LocalizationService.FormatLocalize(m_value, args);
		}

		public override string ToString()
		{
			Debug.LogError("[Localization] Key.ToString() prohibited! Value:\"" + KeyValue + "\"");
			return m_value;
		}
	}

	public class MultyKey : Key
	{
		public MultyKey(string s)
			: base(s)
		{
		}

		public string AddLocalized(string postfix)
		{
			return LocalizationService.Localize(m_value + postfix);
		}

		public MultyKey Add(string[] names, Func<string, string> convertor = null)
		{
			return this;
		}

		public MultyKey Add(Type enumType, Func<string, string> convertor = null)
		{
			return this;
		}

		public MultyKey Add<T>(Func<T, string> convertor = null)
		{
			return this;
		}

		public MultyKey Add(int start, int endInclusive, string format)
		{
			return this;
		}

		public MultyKey Add(int start, int endInclusive)
		{
			return this;
		}

		public MultyKey Remove(string key)
		{
			return this;
		}

		public MultyKey Remove(string[] keys)
		{
			return this;
		}
	}

	public static readonly Key Loading = "loading";

	public static readonly Key Ok = "ok";

	public static readonly Key Cancel = "cancel";

	public static readonly Key Exit = "exit";

	public static readonly Key Close = "close";

	public static readonly Key Use = "use";

	public static readonly Key Yes = "yes";

	public static readonly Key No = "no";

	public static readonly Key Next = "next";

	public static readonly Key error = "error";

	public static readonly Key please_wait = "please_wait";

	public static readonly Key apply_changes_and_restart = "apply_changes_and_restart";

	public static readonly Key exit_question = "exit_question";

	public static readonly Key subroom_quit_question = "subroom_quit_question";

	public static readonly Key no_internet_connection = "no_internet_connection";

	public static readonly Key no_server_connection = "no_server_connection";

	public static readonly Key no_server_connection2 = "no_server_connection2";

	public static readonly Key unknown_error = "unknown_error";

	public static readonly Key league_caps = "league_caps";

	public static readonly Key Time = "time";

	public static readonly Key ShopBuy = "shop_buy";

	public static readonly Key menu_pause_title = "menu_pause_title";

	public static readonly Key replay_play = "replay_play";

	public static readonly Key window_title_race_finish = "window_title_race_finish";

	public static readonly Key garage_window_title = "garage_window_title";

	public static readonly Key garage_setup_km_h = "garage_setup_km_h";

	public static readonly Key garage_setup_second = "garage_setup_second";

	public static readonly Key garage_setup_hp = "garage_setup_hp";

	public static readonly Key garage_setup_rpm = "garage_setup_rpm";

	public static readonly Key garage_setup_nm = "garage_setup_nm";

	public static readonly Key garage_setup_kg = "garage_setup_kg";

	public static readonly Key garage_setup_power_density = "garage_setup_power_density";

	public static readonly Key lobby_filter_power_density_limitation = "lobby_filter_power_density_limitation";

	public static readonly Key track_mode_drift_title = "track_mode_drift_title";

	public static readonly Key track_mode_time_attack_title = "track_mode_time_attack_title";

	public static readonly Key track_mode_training_title = "track_mode_training_title";

	public static readonly Key track_mode_matchmaking_title = "track_mode_matchmaking_title";

	public static readonly Key track_mode_dyno_title = "track_mode_dyno_title";

	public static readonly Key track_mode_xds_title = "track_mode_xds_title";

	public static readonly Key mp_qualify_end_ghost_life_time = "mp_qualify_end_ghost_life_time";

	public static readonly Key mp_qualify_end_ghost_expired = "mp_qualify_end_ghost_expired";

	public static readonly Key mp_no_rating = "mp_no_rating";

	public static readonly Key multiplayer_top_best = "multiplayer_top_best";

	public static readonly Key cup_name_gold = "cup_name_gold";

	public static readonly Key cup_name_silver = "cup_name_silver";

	public static readonly Key cup_name_bronze = "cup_name_bronze";

	public static readonly Key race_result_out_of_time = "race_result_out_of_time";

	public static readonly Key photo_session_cam_target_center_caps = "photo_session_cam_target_center_caps";

	public static readonly Key photo_session_cam_target_fl_caps = "photo_session_cam_target_fl_caps";

	public static readonly Key photo_session_cam_target_fr_caps = "photo_session_cam_target_fr_caps";

	public static readonly Key photo_session_cam_target_rl_caps = "photo_session_cam_target_rl_caps";

	public static readonly Key photo_session_cam_target_rr_caps = "photo_session_cam_target_rr_caps";

	public static readonly Key photo_session_zoom = "photo_session_zoom";

	public static readonly Key error_corrupted_server_response = "error_corrupted_server_response";

	public static readonly Key error_server_error = "error_server_error";

	public static readonly Key error_server_olerloaded_try_again_later = "error_server_olerloaded_try_again_later";

	public static readonly Key error_purchase_failed_2 = "error_purchase_failed_2";

	public static readonly Key error_purchase_failed_try_again_2 = "error_purchase_failed_try_again_2";

	public static readonly Key settings_graphics_quality_veryhigh = "settings_graphics_quality_veryhigh";

	public static readonly Key settings_graphics_quality_high = "settings_graphics_quality_high";

	public static readonly Key settings_graphics_quality_normal = "settings_graphics_quality_normal";

	public static readonly Key settings_graphics_quality_low = "settings_graphics_quality_low";

	public static readonly Key settings_game_inclinometer_autohide = "settings_game_inclinometer_autohide";

	public static readonly Key settings_game_inclinometer_on = "settings_game_inclinometer_on";

	public static readonly Key settings_game_inclinometer_off = "settings_game_inclinometer_off";

	public static readonly Key sync_not_connect_extended = "sync_not_connect_extended";

	public static readonly Key sync_not_connect = "sync_not_connect";

	public static readonly Key multiplayer_available = "multiplayer_available";

	public static readonly Key multiplayer_unavailable = "multiplayer_unavailable";

	public static readonly Key god_voice_wrong_room_pas = "god_voice_wrong_room_pas";

	public static readonly Key god_voice_chat_error = "god_voice_chat_error";

	public static readonly Key god_voice_failed_invite = "god_voice_failed_invite";

	public static readonly Key god_voice_invite_sent = "god_voice_invite_sent";

	public static readonly Key god_voice_invalid_controller_user = "god_voice_invalid_controller_user";

	public static readonly Key god_voice_not_connect_chat_server = "god_voice_not_connect_chat_server";

	public static readonly Key god_voice_autojoin = "god_voice_autojoin";

	public static readonly Key god_voice_autojoin_invitation = "god_voice_autojoin_invitation";

	public static readonly Key god_voice_autojoin_activity = "god_voice_autojoin_activity";

	public static readonly Key god_voice_you_here = "god_voice_you_here";

	public static readonly Key network_room_not_exist = "network_room_not_exist";

	public static readonly Key invite_text = "invite_text";

	public static readonly Key invite_attachement_data_text = "invite_attachement_data_text";

	public static readonly Key invite_description = "invite_description";

	public static readonly Key invite_parse_error = "invite_parse_error";

	public static readonly Key invite_send_error = "invite_send_error";

	public static readonly Key chat_warning_muted = "chat_warning_muted";

	public static readonly Key mp_joined_friend = "mp_joined_friend";

	public static readonly Key mp_joined_friend_by_invitation = "mp_joined_friend_by_invitation";

	public static readonly Key mp_joined_admin = "mp_joined_admin";

	public static readonly Key mp_ban_date_msg = "mp_ban_date_msg";

	public static readonly Key admin_restricted_cross_mp = "admin_restricted_cross_mp";

	public static readonly Key whats_new = "whats_new";

	public static readonly Key patch_note = "patch_note";

	public static readonly Key button_caption_start_game = "button_caption_start_game";

	public static readonly Key button_caption_visit_website = "button_caption_visit_website";

	public static readonly Key localization_id = "localization_id";

	public static readonly Key activity_feed_bought_car = "activity_feed_bought_car";

	public static readonly Key activity_feed_bought_track = "activity_feed_bought_track";

	public static readonly Key activity_feed_done_network_races = "activity_feed_done_network_races";

	public static readonly Key activity_feed_level_unlocked = "activity_feed_level_unlocked";

	public static readonly Key activity_feed_friend_races = "activity_feed_friend_races";

	public static readonly Key activity_feed_gain_race_points = "activity_feed_gain_race_points";

	public static readonly Key activity_feed_single_tandem_leader = "activity_feed_single_tandem_leader";

	public static readonly Key activity_feed_single_tandem_total = "activity_feed_single_tandem_total";

	public static readonly Key activity_feed_win_mp_tandem = "activity_feed_win_mp_tandem";

	public static readonly Key activity_feed_win_mp_tandem_xds = "activity_feed_win_mp_tandem_xds";

	public static readonly Key condensed_activity_feed_bought_car = "condensed_activity_feed_bought_car";

	public static readonly Key condensed_activity_feed_bought_track = "condensed_activity_feed_bought_track";

	public static readonly Key condensed_activity_feed_done_network_races = "condensed_activity_feed_done_network_races";

	public static readonly Key condensed_activity_feed_level_unlocked = "condensed_activity_feed_level_unlocked";

	public static readonly Key condensed_activity_feed_friend_races = "condensed_activity_feed_friend_races";

	public static readonly Key condensed_activity_feed_gain_race_points = "condensed_activity_feed_gain_race_points";

	public static readonly Key condensed_activity_feed_single_tandem_leader = "condensed_activity_feed_single_tandem_leader";

	public static readonly Key condensed_activity_feed_single_tandem_total = "condensed_activity_feed_single_tandem_total";

	public static readonly Key condensed_activity_feed_win_mp_tandem = "condensed_activity_feed_win_mp_tandem";

	public static readonly Key condensed_activity_feed_win_mp_tandem_xds = "condensed_activity_feed_win_mp_tandem_xds";

	public static readonly Key xbox_need_multiplayer_privilege = "xbox_need_multiplayer_privilege";

	public static readonly Key need_signin_to_psn = "need_signin_to_psn";

	public static readonly Key need_psplus_subscription = "need_psplus_subscription";

	public static readonly Key network_room_change_name_title = "network_room_change_name_title";

	public static readonly Key network_game_join_hint = "network_game_join_hint";

	public static readonly Key network_game_join_edit = "network_game_join_edit";

	public static readonly Key network_game_btn_join = "network_game_btn_join";

	public static readonly Key network_game_room_name = "network_game_room_name";

	public static readonly Key network_room_state_waitingstart = "network_room_state_waitingstart";

	public static readonly Key network_room_state_waitingplayers = "network_room_state_waitingplayers";

	public static readonly Key connection_restored = "connection_restored";

	public static readonly Key age_restriction_error = "age_restriction_error";

	public static readonly Key age_restriction_error_extended = "age_restriction_error_extended";

	public static readonly Key need_patch_update = "need_patch_update";

	public static readonly Key need_patch_update_extended = "need_patch_update_extended";

	public static readonly Key dualshock_gamepad_name = "dualshock_gamepad_name";

	public static readonly Key ps4_dualshock_gamepad_name = "ps4_dualshock_gamepad_name";

	public static readonly Key ps5_dualsense_gamepad_name = "ps5_dualsense_gamepad_name";

	public static readonly Key input_action_freecamview = "input_action_freecamview";

	public static readonly Key input_action_racestats = "input_action_racestats";

	public static readonly Key input_action_votemap = "input_action_votemap";

	public static readonly Key input_action_racecreate = "input_action_racecreate";

	public static readonly Key input_action_racejoin = "input_action_racejoin";

	public static readonly Key input_action_createwaypoint = "input_action_createwaypoint";

	public static readonly Key input_action_teleporttowaypoint = "input_action_teleporttowaypoint";

	public static readonly Key input_action_showradialmenu = "input_action_showradialmenu";

	public static readonly Key input_action_gotodynostand = "input_action_gotodynostand";

	public static readonly Key waypoint_is_created = "waypoint_is_created";

	public static readonly Key waypoint_is_not_created = "waypoint_is_not_created";

	public static readonly Key teleport_to_player = "teleport_to_player";

	public static readonly Key teleport_is_not_possible = "teleport_is_not_possible";

	public static readonly Key you_are_teleported = "you_are_teleported";

	public static readonly Key delete_waypoint = "delete_waypoint";

	public static readonly Key bind_left_stick_x = "bind_left_stick_x";

	public static readonly Key bind_left_stick_y = "bind_left_stick_y";

	public static readonly Key bind_right_stick_x = "bind_right_stick_x";

	public static readonly Key bind_right_stick_y = "bind_right_stick_y";

	public static readonly Key bind_wheel = "bind_wheel";

	public static readonly Key bind_accelerator = "bind_accelerator";

	public static readonly Key bind_brake = "bind_brake";

	public static readonly Key bind_clutch = "bind_clutch";

	public static readonly Key bind_handBrake = "bind_handBrake";

	public static readonly Key bind_right_trigger = "bind_right_trigger";

	public static readonly Key bind_left_trigger = "bind_left_trigger";

	public static readonly Key bind_r1_button = "bind_r1_button";

	public static readonly Key bind_r2_button = "bind_r2_button";

	public static readonly Key bind_l1_button = "bind_l1_button";

	public static readonly Key bind_l2_button = "bind_l2_button";

	public static readonly Key afk_kick_warn = "afk_kick_warn";

	public static readonly Key afk_kick_reason = "afk_kick_reason";

	public static readonly Key cant_spawn_car = "cant_spawn_car";

	public static readonly Key chat_room_channel_caption = "chat_room_channel_caption";

	public static readonly Key chat_direct_message_channel_caption = "chat_direct_message_channel_caption";

	public static readonly Key chat_write_to_direct_message = "chat_write_to_direct_message";

	public static readonly Key chat_block_user = "chat_block_user";

	public static readonly Key chat_report_user = "chat_report_user";

	public static readonly Key car_profile_dyno_state_caption = "car_profile_dyno_state_caption";

	public static readonly MultyKey car_profile_category_caption_ = new MultyKey("car_profile_category_caption_{0}").Add(typeof(CarProfileControllerType), (string e) => "car_profile_category_caption_" + e.ToLower()).Remove("car_profile_category_caption_none");

	public static readonly MultyKey CarProfileParameterCaption = new MultyKey("car_profile_prm_caption_{0}").Add(new string[66]
	{
		"brake_front_percent", "final_drive", "front_ackerman", "front_base_offset", "front_camber", "front_caster", "front_disc_diam", "front_lock", "front_spring_stiffness", "front_spring_way",
		"front_stabilizer", "front_tire_type", "front_toe", "front_tyre_pressure", "front_tyre_profile", "front_tyre_width", "gearbox1", "gearbox2", "gearbox3", "gearbox4",
		"gearbox5", "gearbox6", "gearbox7", "gearbox8", "max_brake_torque", "max_torque", "max_torque_rpm", "rear_base_offset", "rear_camber", "rear_disc_diam",
		"rear_spring_stiffness", "rear_spring_way", "rear_stabilizer", "front_fast_bump", "front_fast_rebound", "front_slow_bump", "front_slow_rebound", "rear_fast_bump", "rear_fast_rebound", "rear_slow_bump",
		"rear_slow_rebound", "rear_tire_type", "rear_toe", "rear_tyre_pressure", "rear_tyre_profile", "rear_tyre_width", "rev_limiter", "turbo_charged", "turbo_pressure", "brake_abs",
		"traction_control", "seq_transmision_sound", "wastegate_sound", "diff_type", "diff_lock_coef", "diff_preload", "diff_lock_power", "diff_lock_coast", "front_kingpin", "front_wheel_center_trail",
		"drivetrain_type", "mass", "weight_front_percent", "weight_up_percent", "front_tire_stretch", "rear_tire_stretch"
	}, (string v) => "car_profile_prm_caption_" + v);

	public static readonly MultyKey car_profile_prm_description_ = new MultyKey("car_profile_prm_description_{0}").Add(new string[57]
	{
		"brake_front_percent", "final_drive", "front_ackerman", "front_base_offset", "front_camber", "front_caster", "front_disc_diam", "front_lock", "front_spring_stiffness", "front_spring_way",
		"front_stabilizer", "front_tire_type", "front_toe", "front_tyre_pressure", "front_tyre_profile", "front_tyre_width", "max_brake_torque", "max_torque", "max_torque_rpm", "rear_base_offset",
		"rear_camber", "rear_disc_diam", "rear_spring_stiffness", "rear_spring_way", "rear_stabilizer", "front_fast_bump", "front_fast_rebound", "front_slow_bump", "front_slow_rebound", "rear_fast_bump",
		"rear_fast_rebound", "rear_slow_bump", "rear_slow_rebound", "rear_tire_type", "rear_toe", "rear_tyre_pressure", "rear_tyre_profile", "rear_tyre_width", "rev_limiter", "turbo_charged",
		"turbo_pressure", "brake_abs", "traction_control", "seq_transmision_sound", "diff_type", "diff_lock_coef", "diff_preload", "diff_lock_power", "diff_lock_coast", "front_kingpin",
		"front_wheel_center_trail", "drivetrain_type", "mass", "weight_front_percent", "weight_up_percent", "front_tire_stretch", "rear_tire_stretch"
	}, (string v) => "car_profile_prm_description_" + v);

	public static readonly MultyKey car_profile_prm_units_ = new MultyKey("car_profile_prm_units_{0}").Add(new string[21]
	{
		"atm", "cm", "deg", "hp", "inch", "km_h", "kN_m", "kn_m", "kpa", "lbft",
		"mm", "n_m", "n_sm", "no", "percent", "rpm", "sec", "yes", "n_m_s_rad", "kg",
		"hp_kg"
	}, (string v) => "car_profile_prm_units_" + v);

	public static readonly MultyKey car_profile_diff_type = new MultyKey("car_profile_diff_type_{0}").Add(new string[4] { "open", "welded", "viscous", "clutchtype" }, (string v) => "car_profile_diff_type_" + v);

	public static readonly Key car_profile_prm_caption_dyno_front_tire_type = "car_profile_prm_caption_dyno_front_tire_type";

	public static readonly Key car_profile_prm_units_yes = "car_profile_prm_units_yes";

	public static readonly Key car_profile_prm_units_no = "car_profile_prm_units_no";

	public static readonly Key car_profile_seq_transmission = "car_profile_seq_transmission";

	public static readonly Key car_profile_standart_transmission = "car_profile_standart_transmission";

	public static readonly Key car_profile_parameter_revert = "car_profile_parameter_revert";

	public static readonly Key car_profile_prm_units_lbft = "car_profile_prm_units_lbft";

	public static readonly Key car_profile_prm_units_hp = "car_profile_prm_units_hp";

	public static readonly Key car_profile_prm_units_km_h = "car_profile_prm_units_km_h";

	public static readonly Key car_profile_prm_units_sec = "car_profile_prm_units_sec";

	public static readonly Key car_profile_engine_accel = "garage_setup_accel";

	public static readonly Key car_garage_vendor_filter = "car_garage_vendor_filter";

	public static readonly Key car_profile_window_save_changes_caption = "car_profile_window_save_changes_caption";

	public static readonly Key car_profile_window_save_changes_text = "car_profile_window_save_changes_text";

	public static readonly Key car_profile_window_category_disabled_caption = "car_profile_window_category_disabled_caption";

	public static readonly Key car_profile_window_category_disabled_text = "car_profile_window_category_disabled_text";

	public static readonly MultyKey car_castomize_category_caption = new MultyKey("customize_category_{0}").Add(new string[22]
	{
		"wheel", "chassis", "lightsfront", "bumperfront", "bonnet", "wing", "roof", "mirrors", "doors", "cage",
		"skirts", "trunk", "bumperrear", "spoiler", "lightsrear", "exhaust", "handbrake", "seatleft", "seatright", "shifter",
		"steeringwheel", "windows"
	}, (string v) => "customize_category_" + v);

	public static readonly Key HintText1 = "tutorial2_1_text";

	public static readonly Key HintText2 = "tutorial2_2_text";

	public static readonly Key HintText3 = "tutorial2_3_text";

	public static readonly Key HintText4 = "tutorial2_4_text";

	public static readonly Key HintText5 = "tutorial2_5_text";

	public static readonly Key HintText6 = "tutorial2_6_text";

	public static readonly Key HintText7 = "tutorial2_7_text";

	public static readonly Key HintText8 = "tutorial2_8_text";

	public static readonly Key HintText9 = "tutorial2_9_text";

	public static readonly Key HintText10 = "tutorial2_10_text";

	public static readonly Key HintText11 = "tutorial2_11_text";

	public static readonly Key HintText12 = "tutorial2_12_text";

	public static readonly Key HintText13 = "tutorial2_13_text";

	public static readonly Key HintText14 = "tutorial2_14_text";

	public static readonly Key HintText15 = "tutorial2_15_text";

	public static readonly Key HintText16 = "tutorial2_16_text";

	public static readonly Key HintText17 = "tutorial2_17_text";

	public static readonly Key HintText18 = "tutorial2_18_text";

	public static readonly Key HintText19 = "tutorial2_19_text";

	public static readonly Key HintText20 = "tutorial2_20_text";

	public static readonly Key RatingChallengeTimerStartRace = "rating_challenge_timer_start_race";

	public static readonly Key RatingChallengeTimerNextRace = "rating_challenge_timer_next_race";

	public static readonly Key RatingChallengeTimerExit = "rating_challenge_timer_exit";

	public static readonly Key allowed_axises = "allowed_axises";

	public static readonly Key allowed_buttons = "allowed_buttons";

	public static readonly Key camera_orbit_stick_disable = "camera_orbit_stick_disable";

	public static readonly Key camera_orbit_stick_left = "camera_orbit_stick_left";

	public static readonly Key camera_orbit_stick_right = "camera_orbit_stick_right";

	public static readonly Key catalog_title = "catalog_title";

	public static readonly Key challenges = "challenges";

	public static readonly Key chat_welcome = "chat_welcome";

	public static readonly Key cht_global = "cht_global";

	public static readonly Key cht_room = "cht_room";

	public static readonly Key confirm = "confirm";

	public static readonly Key control_off = "control_off";

	public static readonly Key control_on = "control_on";

	public static readonly Key credits = "credits";

	public static readonly Key customize_category_wheel = "customize_category_wheel";

	public static readonly Key customization_body = "customization_body";

	public static readonly Key customization_paint = "customization_paint";

	public static readonly Key car_customize_tires = "car_customize_tires";

	public static readonly Key car_customize_no_favorite_rims = "car_customize_no_favorite_rims";

	public static readonly Key design_title = "design_title";

	public static readonly Key dublicate_layer = "dublicate_layer";

	public static readonly Key flip_layer = "flip_layer";

	public static readonly Key friends_title = "friends_title";

	public static readonly Key game_wnd_voteban = "game_wnd_voteban";

	public static readonly Key game_wnd_votekick = "game_wnd_votekick";

	public static readonly Key game_wnd_votemap = "game_wnd_votemap";

	public static readonly Key game_wnd_votenight = "game_wnd_votenight";

	public static readonly Key game_wnd_votelight = "game_wnd_votelight";

	public static readonly Key default_light_preset = "default_light_preset";

	public static readonly Key cant_vote_light = "cant_vote_light";

	public static readonly Key network_game_params_light_mode = "network_game_params_light_mode";

	public static readonly Key garage_hint_profile_dyno_stock = "garage_hint_profile_dyno_stock";

	public static readonly Key garage_switch_next_car = "garage_switch_next_car";

	public static readonly Key garage_switch_prev_car = "garage_switch_prev_car";

	public static readonly Key global_rating_title = "global_rating_title";

	public static readonly Key global_rating_top_500 = "global_rating_top_500";

	public static readonly Key input_action_hideui = "input_action_hideui";

	public static readonly Key input_action_nextcamera = "input_action_nextcamera";

	public static readonly Key input_hex_code = "input_hex_code";

	public static readonly Key input_action_flares = "input_action_flares";

	public static readonly Key input_settings_calibration_warning = "input_settings_calibration_warning";

	public static readonly Key input_settings_calibration_inverse_axis = "input_settings_calibration_inverse_axis";

	public static readonly Key input_settings_first_calibration_warning = "input_settings_first_calibration_warning";

	public static readonly Key input_settings_need_wheel_calibration = "input_settings_need_wheel_calibration";

	public static readonly Key input_settings_no_available_assistant_in_current_room = "input_settings_no_available_assistant_in_current_room";

	public static readonly Key input_settings_description_title = "input_settings_description_title";

	public static readonly Key input_settings_device_description_title = "input_settings_device_description_title";

	public static readonly Key input_settings_devices_main = "input_settings_devices_main";

	public static readonly Key input_settings_devices_advanced = "input_settings_devices_advanced";

	public static readonly Key input_settings_devices_category = "input_settings_devices_category";

	public static readonly Key input_settings_input_max_value_text = "input_settings_input_max_value_text";

	public static readonly Key input_settings_input_min_value_text = "input_settings_input_min_value_text";

	public static readonly Key input_settings_accelerometer_description = "input_settings_accelerometer_description";

	public static readonly Key ps4_input_settings_accelerometer_description = "ps4_input_settings_accelerometer_description";

	public static readonly Key kit_wheels_customize = "kit_wheels_customize";

	public static readonly Key layer_change_discard = "layer_change_discard";

	public static readonly Key layer_symmetry_on = "layer_symmetry_on";

	public static readonly Key layer_symmetry_off = "layer_symmetry_оff";

	public static readonly Key layer_groupmask_body = "layer_groupmask_body";

	public static readonly Key layer_groupmask_glass = "layer_groupmask_glass";

	public static readonly Key layer_groupmask_all = "layer_groupmask_all";

	public static readonly Key loading_steam = "loading_steam";

	public static readonly Key loading_sync = "loading_sync";

	public static readonly Key loading_car = "loading_car";

	public static readonly Key lvlup_paintcar = "lvlup_paintcar";

	public static readonly Key lvlup_paintglass = "lvlup_paintglass";

	public static readonly Key lvlup_paintrim = "lvlup_paintrim";

	public static readonly Key lvlup_paintsmoke = "lvlup_paintsmoke";

	public static readonly Key lvlup_paintspokes = "lvlup_paintspokes";

	public static readonly Key matchmaking_leave_queue = "matchmaking_leave_queue";

	public static readonly Key matchmaking_unavailable = "matchmaking_unavailable";

	public static readonly Key menu_pause_photosession = "menu_pause_photosession";

	public static readonly Key menu_pause_finish_race = "menu_pause_finish_race";

	public static readonly Key mod_wrong_version = "mod_wrong_version";

	public static readonly Key not_allowed_mod_chassis = "not_allowed_mod_chassis";

	public static readonly Key mod_wrong_version_info = "mod_wrong_version_info";

	public static readonly Key mod_wrong_version_info2 = "mod_wrong_version_info2";

	public static readonly Key mp_battle_end_result = "mp_battle_end_result";

	public static readonly Key minimap_increase_size = "minimap_increase_size";

	public static readonly Key minimap_decrease_size = "minimap_decrease_size";

	public static readonly Key nav_add_controlls = "nav_add_controlls";

	public static readonly Key nav_hint_add_layer = "nav_hint_add_layer";

	public static readonly Key nav_hint_apply = "nav_hint_apply";

	public static readonly Key nav_hint_continue = "nav_hint_continue";

	public static readonly Key nav_hint_dublicate = "nav_hint_dublicate";

	public static readonly Key nav_hint_dyno = "nav_hint_dyno";

	public static readonly Key nav_hint_edit = "nav_hint_edit";

	public static readonly Key nav_hint_edit_color = "nav_hint_edit_color";

	public static readonly Key nav_hint_exit = "nav_hint_exit";

	public static readonly Key nav_hint_exit_watch_mode = "nav_hint_exit_watch_mode";

	public static readonly Key nav_hint_favorite_colors = "nav_hint_favorite_colors";

	public static readonly Key nav_hint_goto_back = "nav_hint_goto_back";

	public static readonly Key nav_hint_goto_device_settings = "nav_hint_goto_device_settings";

	public static readonly Key nav_hint_goto_testdrive = "nav_hint_goto_testdrive";

	public static readonly Key nav_hint_input_settings_advanced_reset = "nav_hint_input_settings_advanced_reset";

	public static readonly Key nav_hint_input_settings_advanced_reset_all = "nav_hint_input_settings_advanced_reset_all";

	public static readonly Key nav_hint_input_settings_bind = "nav_hint_input_settings_bind";

	public static readonly Key nav_hint_input_settings_calibration = "nav_hint_input_settings_calibration";

	public static readonly Key switch_disable_ffb = "switch_disable_ffb";

	public static readonly Key nav_hint_input_settings_calibration_begin = "nav_hint_input_settings_calibration_begin";

	public static readonly Key nav_hint_input_settings_calibration_end = "nav_hint_input_settings_calibration_end";

	public static readonly Key nav_hint_input_settings_calibration_save = "nav_hint_input_settings_calibration_save";

	public static readonly Key nav_hint_input_settings_calibration_inversion = "nav_hint_input_settings_calibration_inversion";

	public static readonly Key nav_hint_input_settings_remove_bind = "nav_hint_input_settings_remove_bind";

	public static readonly Key nav_hint_invite = "nav_hint_invite";

	public static readonly Key nav_hint_invtite_friend = "nav_hint_invtite_friend";

	public static readonly Key nav_hint_override = "nav_hint_override";

	public static readonly Key nav_hint_remove_layer = "nav_hint_remove_layer";

	public static readonly Key nav_hint_remove_slot = "nav_hint_remove_slot";

	public static readonly Key nav_hint_replace_layer = "nav_hint_replace_layer";

	public static readonly Key nav_hint_replay = "nav_hint_replay";

	public static readonly Key nav_hint_reset = "nav_hint_reset";

	public static readonly Key nav_hint_reset_all = "nav_hint_reset_all";

	public static readonly Key nav_hint_reset_camera = "nav_hint_reset_camera";

	public static readonly Key nav_hint_restart = "nav_hint_restart";

	public static readonly Key nav_hint_select = "nav_hint_select";

	public static readonly Key nav_hint_switch_page = "nav_hint_switch_page";

	public static readonly Key nav_hint_watch = "nav_hint_watch";

	public static readonly Key nav_hint_buy = "nav_hint_buy";

	public static readonly Key nav_hint_day = "nav_hint_day";

	public static readonly Key nav_hint_night = "nav_hint_night";

	public static readonly Key network_game_btn_create = "network_game_btn_create";

	public static readonly Key network_game_btn_joinpass = "network_game_btn_joinpass";

	public static readonly Key network_game_params_clipping_mode = "network_game_params_clipping_mode";

	public static readonly Key network_game_params_collisions_enable = "network_game_params_collisions_enable";

	public static readonly Key network_game_params_drift_mode = "network_game_params_drift_mode";

	public static readonly Key network_game_params_password = "network_game_params_password";

	public static readonly Key network_game_params_timeattack_mode = "network_game_params_timeattack_mode";

	public static readonly Key network_game_params_track = "network_game_params_track";

	public static readonly Key network_game_params_spawnpoints = "network_game_params_spawnpoints";

	public static readonly Key network_game_params_assist = "network_game_params_assist";

	public static readonly Key netwotk_game_params_traction_control = "netwotk_game_params_traction_control";

	public static readonly Key network_game_params_drivetrain_type = "network_game_params_drivetrain_type";

	public static readonly Key network_game_params_vote_enable = "network_game_params_vote_enable";

	public static readonly Key network_game_params_night_mode = "network_game_params_night_mode";

	public static readonly Key network_game_params_adh_limit = "network_game_params_adh_limit";

	public static readonly Key network_game_params_adh_limit_short = "network_game_params_adh_limit_short";

	public static readonly Key network_game_change_adh_to_continue = "network_game_change_adh_to_continue";

	public static readonly Key network_game_change_asisst_to_continue = "network_game_change_asisst_to_continue";

	public static readonly Key network_game_unable_to_create_room_with_adh = "network_game_unable_to_create_room_with_adh";

	public static readonly Key network_game_failed_set_car_assist = "network_game_failed_set_car_assist";

	public static readonly Key network_game_params_adh_all = "network_game_params_adh_all";

	public static readonly Key network_game_params_without_assist = "network_game_params_without_assist";

	public static readonly Key network_game_params_traction_control = "network_game_params_traction_control";

	public static readonly Key network_game_room_full = "network_game_room_full";

	public static readonly Key network_game_servername_default = "network_game_servername_default";

	public static readonly Key network_game_power_density_room_compatibility_error = "network_game_power_density_room_compatibility_error";

	public static readonly Key network_game_room_type = "network_game_room_type";

	public static readonly Key network_game_drivetrain_type_room_compatibility_error = "network_game_drivetrain_type_room_compatibility_error";

	public static readonly Key network_game_drivetrain_type_room_compatibility_error_ask_change = "network_game_drivetrain_type_room_compatibility_error_ask_change";

	public static readonly Key network_game_critical_params_room_compatibility_error = "network_game_critical_params_room_compatibility_error";

	public static readonly Key network_game_available_on_racing_setup_message = "network_game_available_on_racing_setup_message";

	public static readonly Key network_game_available_on_can_tuned_setup_message = "network_game_available_on_racing_setup_message";

	public static readonly Key network_game_power_density_explanation_parameter_change = "network_game_power_density_explanation_parameter_change";

	public static readonly Key network_game_traction_control_room_compatibility_error_ask_change = "network_game_traction_control_room_compatibility_error_ask_change";

	public static readonly Key сollisions_classic = "сollisions_classic";

	public static readonly Key collisions_dynamic = "collisions_dynamic";

	public static readonly Key network_race_player = "network_race_player";

	public static readonly Key network_race_player_disqualified = "network_race_player_disqualified";

	public static readonly Key player_disqualified = "player_disqualified";

	public static readonly Key not_enough_money = "not_enough_money";

	public static readonly Key player_profile_reset_stats_message = "player_profile_reset_stats_message";

	public static readonly Key player_profile_stats_kilometers = "player_profile_stats_kilometers";

	public static readonly Key player_profile_stats_kilometers_in_drift = "player_profile_stats_kilometers_in_drift";

	public static readonly Key player_profile_stats_mile = "player_profile_stats_mile";

	public static readonly Key player_profile_stats_mile_in_drift = "player_profile_stats_mile_in_drift";

	public static readonly Key player_profile_stats_unit_kmh = "player_profile_stats_unit_kmh";

	public static readonly Key player_profile_stats_unit_mph = "player_profile_stats_unit_mph";

	public static readonly Key player_profile_wnd = "player_profile_wnd";

	public static readonly Key prevention_discord_message = "prevention_discord_message";

	public static readonly Key prevention_epic_games_message = "prevention_epic_games_message";

	public static readonly Key race_stats_title = "race_stats_title";

	public static readonly Key rating_challenge_disqualified = "rating_challenge_disqualified";

	public static readonly Key rating_challenge_finished = "rating_challenge_finished";

	public static readonly Key rating_challenge_inactive = "rating_challenge_inactive";

	public static readonly Key rating_challenge_loading = "rating_challenge_loading";

	public static readonly Key rating_challenge_qualifiedraces = "rating_challenge_qualifiedraces";

	public static readonly Key rating_position_title = "rating_position_title";

	public static readonly Key rearview_mirror_text = "rearview_mirror_text";

	public static readonly Key reset = "reset";

	public static readonly Key reset_player_progess = "reset_player_progess";

	public static readonly Key screen_title_multiplayer = "screen_title_multiplayer";

	public static readonly Key screen_title_settings = "screen_title_settings";

	public static readonly Key screen_title_singleplayer = "screen_title_singleplayer";

	public static readonly Key settings_category_steer_title = "settings_category_steer_title";

	public static readonly Key settings_info_empty_accel_bind_with_wheel_connected = "settings_info_empty_accel_bind_with_wheel_connected";

	public static readonly Key settings_controls = "settings_controls";

	public static readonly Key settings_controls_st_assist = "settings_controls_st_assist";

	public static readonly Key settings_controls_st_assist_wheel = "settings_controls_st_assist_wheel";

	public static readonly Key settings_controls_st_assist_wheel_as_gamepad = "settings_controls_st_assist_wheel_as_gamepad";

	public static readonly Key settings_controls_st_assist_gamepad = "settings_controls_st_assist_gamepad";

	public static readonly Key settings_controls_st_assist_gamepad_xbox = "settings_controls_st_assist_gamepad_xbox";

	public static readonly Key settings_game_camera_cockpit = "settings_game_camera_cockpit";

	public static readonly Key settings_game_camera_dynamic = "settings_game_camera_dynamic";

	public static readonly Key settings_game_camera_dynamic_mode = "settings_game_camera_dynamic_mode";

	public static readonly Key settings_game_camera_dynamic_mode_legacy = "settings_game_camera_dynamic_mode_legacy";

	public static readonly Key settings_game_camera_dynamic_mode_improved = "settings_game_camera_dynamic_mode_improved";

	public static readonly Key settings_game_camera_highrevs_shaking = "settings_game_camera_highrevs_shaking";

	public static readonly Key settings_game_chat = "settings_game_chat";

	public static readonly Key settings_game_show_nicknames = "settings_game_show_nicknames";

	public static readonly Key settings_game_cross_platform_play = "settings_game_cross_platform_play";

	public static readonly Key settings_game_auto_connect_to_epic = "settings_game_auto_connect_to_epic";

	public static readonly Key settings_game_cross_platform_ps4 = "settings_game_cross_platform_ps4";

	public static readonly Key settings_game_inclinometer = "settings_game_inclinometer";

	public static readonly Key settings_game_lang = "settings_game_lang";

	public static readonly Key settings_game_sghost = "settings_game_sghost";

	public static readonly Key settings_game_sminimap = "settings_game_sminimap";

	public static readonly Key settings_game_speed_unit = "settings_game_speed_unit";

	public static readonly Key settings_game_speed_unit_kmh = "settings_game_speed_unit_kmh";

	public static readonly Key settings_game_speed_unit_mph = "settings_game_speed_unit_mph";

	public static readonly Key settings_game_steer_w_angle = "settings_game_steer_w_angle";

	public static readonly Key settings_game_pedals_indicator = "settings_game_pedals_indicator";

	public static readonly Key settings_game_show_drift_hud = "settings_game_show_drift_hud";

	public static readonly Key settings_game_show_kissthewall_hud = "settings_game_show_kissthewall_hud";

	public static readonly Key settings_game_display_boost_gauge = "settings_game_display_boost_gauge";

	public static readonly Key settings_game_hidewindows = "settings_game_hidewindows";

	public static readonly Key settings_graphics_antialiasing = "settings_graphics_antialiasing";

	public static readonly Key settings_graphics_ssr_description = "settings_graphics_ssr_description";

	public static readonly Key settings_graphics_bloom = "settings_graphics_bloom";

	public static readonly Key settings_graphics_ao = "settings_graphics_ao";

	public static readonly Key settings_graphics_shadows = "settings_graphics_shadows";

	public static readonly Key settings_graphics_motionblur = "settings_graphics_motionblur";

	public static readonly Key settings_graphics_skidmark_length = "settings_graphics_skidmark_length";

	public static readonly Key settings_graphics_skidmark = "settings_graphics_skidmark";

	public static readonly Key settings_graphics_skidmark_description = "settings_graphics_skidmark_description";

	public static readonly Key settings_graphics_texturequality = "settings_graphics_texturequality";

	public static readonly Key settings_graphics_anisolevel = "settings_graphics_anisolevel";

	public static readonly Key settings_graphics_lod = "settings_graphics_lod";

	public static readonly Key settings_graphics_fullscreen = "settings_graphics_fullscreen";

	public static readonly Key settings_graphics_screenMode = "settings_graphics_screenMode";

	public static readonly Key settings_graphics_windowed = "settings_graphics_windowed";

	public static readonly Key settings_graphics_borderlessWindowed = "settings_graphics_borderlessWindowed";

	public static readonly Key settings_graphics_quality = "settings_graphics_quality";

	public static readonly Key settings_graphics_volumetricfog = "settings_graphics_volumetricfog";

	public static readonly Key settings_graphics_resolution = "settings_graphics_resolution";

	public static readonly Key settings_graphics_fps = "settings_graphics_fps";

	public static readonly Key settings_graphics_smoke = "settings_graphics_smoke";

	public static readonly Key settings_graphics_opponent_smoke_opacity = "settings_graphics_opponent_smoke_opacity";

	public static readonly Key settings_graphics_player_smoke_opacity = "settings_graphics_player_smoke_opacity";

	public static readonly Key settings_graphics_opponent_light_opacity = "settings_graphics_opponent_light_opacity";

	public static readonly Key settings_graphics_player_light_opacity = "settings_graphics_player_light_opacity";

	public static readonly Key settings_graphics_enable_vinyl_compression = "settings_graphics_enable_vinyl_compression";

	public static readonly Key settings_graphics_vfov = "settings_graphics_vfov";

	public static readonly Key settings_graphics_vfov_cockpit = "settings_graphics_vfov_cockpit";

	public static readonly Key settings_input_accLinearity = "settings_input_accLinearity";

	public static readonly Key settings_input_brakeLinearity = "settings_input_brakeLinearity";

	public static readonly Key settings_input_camera_orbit_stick = "settings_input_camera_orbit_stick";

	public static readonly Key settings_input_clutchLinearity = "settings_input_clutchLinearity";

	public static readonly Key settings_input_clutch_auto = "settings_input_clutch_auto";

	public static readonly Key settings_input_clutch_manual = "settings_input_clutch_manual";

	public static readonly Key settings_input_clutch_type = "settings_input_clutch_type";

	public static readonly Key settings_input_ignition_type = "settings_input_ignition_type";

	public static readonly Key settings_input_ignition_simplified = "settings_input_ignition_simplified";

	public static readonly Key settings_input_ignition_cockpit = "settings_input_ignition_cockpit";

	public static readonly Key settings_input_cursoracceleration = "settings_input_cursoracceleration";

	public static readonly Key settings_input_feedback = "settings_input_feedback";

	public static readonly Key settings_input_autocenterpar = "settings_input_autocenterpar";

	public static readonly Key settings_input_vibration = "settings_input_vibration";

	public static readonly Key settings_input_active_control_scheme = "settings_input_active_control_scheme";

	public static readonly Key settings_input_vibration_xbox = "settings_input_vibration_xbox";

	public static readonly Key settings_input_gearbox_auto = "settings_input_gearbox_auto";

	public static readonly Key settings_input_gearbox_manual = "settings_input_gearbox_manual";

	public static readonly Key settings_input_gearbox_type = "settings_input_gearbox_type";

	public static readonly Key settings_input_controller_type = "settings_input_controller_type";

	public static readonly Key settings_input_steering_device_type_choose = "settings_input_steering_device_type_choose";

	public static readonly Key settings_input_steering_device_type_choose_description = "settings_input_steering_device_type_choose_description";

	public static readonly Key settings_input_keyboard = "settings_input_keyboard";

	public static readonly Key settings_input_mouse = "settings_input_mouse";

	public static readonly Key settings_input_gamepad = "settings_input_gamepad";

	public static readonly Key settings_input_wheel = "settings_input_wheel";

	public static readonly Key settings_input_handbrakeLinearity = "settings_input_handbrakeLinearity";

	public static readonly Key settings_input_reset_question = "settings_input_reset_question";

	public static readonly Key settings_input_wheelLinearity = "settings_input_wheelLinearity";

	public static readonly Key settings_input_wheeldeadzone = "settings_input_wheeldeadzone";

	public static readonly Key settings_sound_master = "settings_sound_master";

	public static readonly Key settings_sound_music = "settings_sound_music";

	public static readonly Key settings_sound_sound_fx = "settings_sound_sound_fx";

	public static readonly Key settings_sound_additional_fx = "settings_sound_additional_fx";

	public static readonly Key settings_sound_pad_sound = "settings_sound_pad_sound";

	public static readonly Key setups_window_title = "setups_window_title";

	public static readonly Key customs_window_title = "customs_window_title";

	public static readonly Key singlemode_dirft = "singlemode_dirft";

	public static readonly Key singlemode_timeattack = "singlemode_timeattack";

	public static readonly Key singlemode_training = "singlemode_training";

	public static readonly Key singlemode_xds = "singlemode_xds";

	public static readonly Key steering_assist_v1 = "steering_assist_v1";

	public static readonly Key steering_assist_v2 = "steering_assist_v2";

	public static readonly Key swipe_right_to_show_ui = "swipe_right_to_show_ui";

	public static readonly Key vinyls_editing_layers_title = "vinyls_editing_layers_title";

	public static readonly Key vinyls_editing_title = "vinyls_editing_title";

	public static readonly Key vinyls_max_layers = "vinyls_max_layers";

	public static readonly Key vinyls_layers_count = "vinyls_layers_count";

	public static readonly Key vinyls_not_support = "vinyls_not_support";

	public static readonly Key paint_not_support = "paint_not_support";

	public static readonly Key paint_already_purchased = "paint_already_purchased";

	public static readonly Key not_supported = "not_supported";

	public static readonly Key vinyls_title = "vinyls_title";

	public static readonly Key my_vinyls = "my_vinyls";

	public static readonly Key size = "size";

	public static readonly Key placeholder_input_text = "placeholder_input_text";

	public static readonly Key server_sharing_liveries = "server_sharing_liveries";

	public static readonly Key server_sharing_tracks = "server_sharing_tracks";

	public static readonly Key car_sharing_title = "car_sharing_title";

	public static readonly Key sharing_livery_dlc_filter_title = "sharing_livery_dlc_filter_title";

	public static readonly Key sharing_livery_dlc_filter_value = "sharing_livery_dlc_filter_value";

	public static readonly Key save_custom_livery = "save_custom_livery";

	public static readonly Key show_vinyls_option_none = "show_vinyls_option_none";

	public static readonly Key show_vinyls_option_all = "show_vinyls_option_all";

	public static readonly Key show_vinyls_option_current_platform = "show_vinyls_option_current_platform";

	public static readonly Key voteresult_ban_self = "voteresult_ban_self";

	public static readonly Key voteresult_expired = "voteresult_expired";

	public static readonly Key voteresult_kick_self = "voteresult_kick_self";

	public static readonly Key voteresult_map_no = "voteresult_map_no";

	public static readonly Key voteresult_night_no = "voteresult_night_no";

	public static readonly Key vote_few_players = "vote_few_players";

	public static readonly Key vote_cooldown_in_progress = "vote_cooldown_in_progress";

	public static readonly Key vote_is_already_going = "vote_is_already_going";

	public static readonly Key unable_perform_mp_hud_action = "unable_perform_mp_hud_action";

	public static readonly Key window_caption_remove_slot = "window_caption_remove_slot";

	public static readonly Key waiting_opponent = "waiting_opponent";

	public static readonly Key tandem_race_stopped = "tandem_race_stopped";

	public static readonly Key tandem_opponent_disqualified = "tandem_opponent_disqualified";

	public static readonly Key xds_retry_second_race = "xds_retry_second_race";

	public static readonly Key xds_esp_off = "xds_esp_off";

	public static readonly Key xds_esp_on = "xds_esp_on";

	public static readonly Key xds_finish = "xds_finish";

	public static readonly Key xds_initiation = "xds_initiation";

	public static readonly Key xds_rsn_badclipzone = "xds_rsn_badclipzone";

	public static readonly Key xds_rsn_clip = "xds_rsn_clip";

	public static readonly Key xds_one_more_time = "xds_one_more_time";

	public static readonly Key xds_sector = "xds_sector";

	public static readonly Key xds_total = "xds_total";

	public static readonly Key xds_ui_chaser = "xds_ui_chaser";

	public static readonly Key xds_ui_leader = "xds_ui_leader";

	public static readonly Key xds_ui_race1 = "xds_ui_race1";

	public static readonly Key xds_ui_race2 = "xds_ui_race2";

	public static readonly Key xds_ui_select_car = "xds_ui_select_car";

	public static readonly Key xds_ui_select_pressure = "xds_ui_select_pressure";

	public static readonly Key xds_warn_contact = "xds_warn_contact";

	public static readonly Key xds_warn_correction = "xds_warn_correction";

	public static readonly Key xds_warn_lowspeed = "xds_warn_lowspeed";

	public static readonly Key xds_warn_overtake = "xds_warn_overtake";

	public static readonly Key xds_slowdown = "xds_slowdown";

	public static readonly Key xds_cant_create_badconnection = "xds_cant_create_badconnection";

	public static readonly Key xds_cant_join_badconnection = "xds_cant_join_badconnection";

	public static readonly Key vote_started = "vote_started";

	public static readonly Key cht_user_offline = "cht_user_offline";

	public static readonly Key drift_points = "drift_points";

	public static readonly Key dyno_input_value_text = "dyno_input_value_text";

	public static readonly Key dyno_ask_reset_all_to_defaults = "dyno_ask_reset_all_to_defaults";

	public static readonly Key dyno_ask_reset_single_to_default = "dyno_ask_reset_single_to_default";

	public static readonly Key input_settings_device_disconnect = "input_settings_device_disconnect";

	public static readonly Key input_settings_device_column_type = "input_settings_device_column_type";

	public static readonly Key input_settings_element_busy = "input_settings_element_busy";

	public static readonly Key input_settings_textBinding = "input_settings_textBinding";

	public static readonly Key locked_level_message_track = "locked_level_message_track";

	public static readonly Key locked_track_info = "locked_track_info";

	public static readonly Key matchmaking_avg_search_time_title = "matchmaking_avg_search_time_title";

	public static readonly Key matchmaking_players_in_queue_title = "matchmaking_players_in_queue_title";

	public static readonly Key matchmaking_race_time = "matchmaking_race_time";

	public static readonly Key matchmaking_waiting_time = "matchmaking_waiting_time";

	public static readonly Key menu_inroom_pass = "menu_inroom_pass";

	public static readonly Key message_buy = "message_buy";

	public static readonly Key message_buy_track = "message_buy_track";

	public static readonly Key network_notif_create_room = "network_notif_create_room";

	public static readonly Key network_race_msg_finish = "network_race_msg_finish";

	public static readonly Key network_race_msg_nowinner = "network_race_msg_nowinner";

	public static readonly Key network_race_msg_result = "network_race_msg_result";

	public static readonly Key network_race_msg_winner = "network_race_msg_winner";

	public static readonly Key network_room_creator = "network_room_creator";

	public static readonly Key season_info_timer_until_finish_season = "season_info_timer_until_finish_season";

	public static readonly Key season_info_timer_until_next_season = "season_info_timer_until_next_season";

	public static readonly Key voteresult_ban_no = "voteresult_ban_no";

	public static readonly Key voteresult_ban_yes = "voteresult_ban_yes";

	public static readonly Key voteresult_kick_yes = "voteresult_kick_yes";

	public static readonly Key xds_initiation_speed = "xds_initiation_speed";

	public static readonly Key xds_initiation_time = "xds_initiation_time";

	public static readonly Key xds_ui_config = "xds_ui_config";

	public static readonly Key config = "config";

	public static readonly Key xds_room_safemode_on = "xds_room_safemode_on";

	public static readonly Key xds_room_safemode_off = "xds_room_safemode_off";

	public static readonly Key xds_room_record_on = "xds_room_record_on";

	public static readonly Key xds_room_record_off = "xds_room_record_off";

	public static readonly Key subs_last_day = "subs_last_day";

	public static readonly Key subs_last_days = "subs_last_days";

	public static readonly Key subs_last_days2 = "subs_last_days2";

	public static readonly Key subscription_expiring = "subscription_expiring";

	public static readonly Key subscription_date_expiring = "subscription_date_expiring";

	public static readonly Key subscription_dlc_owned_info = "subscription_dlc_owned_info";

	public static readonly Key subscription_main_window_info = "subscription_main_window_info";

	public static readonly Key subscription_active_info = "subscription_active_info";

	public static readonly Key subscription_no_active_info = "subscription_no_active_info";

	public static readonly Key subscription_end_date = "subscription_end_date";

	public static readonly Key subscription_verify_failed_title = "subscription_verify_failed_title";

	public static readonly Key subscription_verify_failed_body = "subscription_verify_failed_body";

	public static readonly Key subscription_days_expiring = "subscription_days_expiring";

	public static readonly Key subscription_check_connect = "subscription_check_connect";

	public static readonly Key settings_discord = "settings_discord";

	public static readonly Key buy_dlc = "buy_dlc";

	public static readonly Key buy_season_pass = "buy_season_pass";

	public static readonly Key no_dlc_track = "no_dlc_track";

	public static readonly Key no_dlc_car = "no_dlc_car";

	public static readonly Key no_dlc_track_join = "no_dlc_track_join";

	public static readonly Key no_dlc_track_goodvoice = "no_dlc_track_goodvoice";

	public static readonly Key no_dlc_night_mode = "no_dlc_night_mode";

	public static readonly Key wrong_room_pass = "wrong_room_pass";

	public static readonly Key wrong_checkpoint_missed = "wrong_checkpoint_missed";

	public static readonly Key return_car_to_last_checkpoint = "return_car_to_last_checkpoint";

	public static readonly Key driftevent_clippingspeed = "driftevent_clippingspeed";

	public static readonly Key autojoin_to_crossplatform_room = "autojoin_to_crossplatform_room";

	public static readonly Key tires_changed_to_winter_type = "tires_changed_to_winter_type";

	public static readonly Key hint_show_ui = "hint_show_ui";

	public static readonly Key change_server = "change_server";

	public static readonly Key server_current = "server_current";

	public static readonly Key cht_popup_overlayprofile = "cht_popup_overlayprofile";

	public static readonly Key server_chosen = "server_chosen";

	public static readonly Key reset_car_help_message = "reset_car_help_message";

	public static readonly Key lobby_all_tracks_filter = "lobby_all_tracks_filter";

	public static readonly Key lobby_filters = "lobby_filters";

	public static readonly Key lobby_search_by_room_name = "lobby_search_by_room_name";

	public static readonly Key lobby_reset_filters = "lobby_reset_filters";

	public static readonly Key lobby_filter_query_not_found = "lobby_filter_query_not_found";

	public static readonly Key lobby_enable_crossmp_more_rooms = "lobby_enable_crossmp_more_rooms";

	public static readonly Key vsync_wheel_disable = "vsync_wheel_disable";

	public static readonly Key part_to_paint_remove = "part_to_paint_remove";

	public static readonly Key part_to_paint_add = "part_to_paint_add";

	public static readonly Key part_reset_color = "part_reset_color";

	public static readonly Key part_multiselect_set_on = "part_multiselect_set_on";

	public static readonly Key part_multiselect_set_off = "part_multiselect_set_off";

	public static readonly Key part_multiselect_on = "part_multiselect_on";

	public static readonly Key part_multiselect_off = "part_multiselect_off";

	public static readonly Key part_all_body = "part_all_body";

	public static readonly Key part_ramp = "part_ramp";

	public static readonly Key part_arm = "part_arm";

	public static readonly Key part_spring = "part_spring";

	public static readonly Key part_strut = "part_strut";

	public static readonly Key part_calliper = "part_calliper";

	public static readonly Key part_front_rim = "part_front_rim";

	public static readonly Key part_front_spokes = "part_front_spokes";

	public static readonly Key part_rear_rim = "part_rear_rim";

	public static readonly Key part_rear_spokes = "part_rear_spokes";

	public static readonly Key part_tiresticker1 = "part_tiresticker1";

	public static readonly Key part_tiresticker2 = "part_tiresticker2";

	public static readonly Key part_steering_wheel_spokes = "part_steering_wheel_spokes";

	public static readonly Key part_steering_wheel_cover = "part_steering_wheel_cover";

	public static readonly Key part_left_seat_base = "part_left_seat_base";

	public static readonly Key part_left_seat_cushion = "part_left_seat_cushion";

	public static readonly Key part_right_seat_base = "part_right_seat_base";

	public static readonly Key part_right_seat_cushion = "part_right_seat_cushion";

	public static readonly Key part_glass = "part_glass";

	public static readonly Key paint_suspension_title = "paint_suspension_title";

	public static readonly Key paint_interior_title = "paint_interior_title";

	public static readonly Key nav_select_report = "nav_select_report";

	public static readonly Key incorrect_room_name_reason = "incorrect_room_name_reason";

	public static readonly Key using_cheats_reason = "using_cheats_reason";

	public static readonly Key griefing_reason = "griefing_reason";

	public static readonly Key bad_vinyls_reason = "bad_vinyls_reason";

	public static readonly Key report_setup_reason = "report_setup_reason";

	public static readonly Key report_reason = "report_reason";

	public static readonly Key report_additional_info = "report_additional_info";

	public static readonly Key report_description_info = "report_description_info";

	public static readonly Key send_report = "send_report";

	public static readonly Key report_sended = "report_sended";

	public static readonly Key report_to_player = "report_to_player";

	public static readonly Key report_select_room = "report_select_room";

	public static readonly Key report_select_setup = "report_select_setup";

	public static readonly Key report_select_livery = "report_select_livery";

	public static readonly Key report_select_player = "report_select_player";

	public static readonly Key player_already_reported = "player_already_reported";

	public static readonly Key rejoin_to_room = "rejoin_to_room";

	public static readonly Key xbox_profile_select = "xbox_profile_select";

	public static readonly Key xbox_profile_changed = "xbox_profile_changed";

	public static readonly Key xbox_game_will_restart = "xbox_game_will_restart";

	public static readonly Key xbox_controller_disconnected = "xbox_controller_disconnected";

	public static readonly Key xbox_reconnect_controller = "xbox_reconnect_controller";

	public static readonly Key xbox_signin_needed = "xbox_signin_needed";

	public static readonly Key xbox_profile_signin = "xbox_profile_signin";

	public static readonly Key settings_sound_voicechat = "settings_sound_voicechat";

	public static readonly Key input_action_voicechat = "input_action_voicechat";

	public static readonly Key freedrone_title_free_camera = "freedrone_title_free_camera";

	public static readonly Key freedrone_title_following_camera = "freedrone_title_following_camera";

	public static readonly Key freedrone_title_tracking_camera = "freedrone_title_tracking_camera";

	public static readonly Key freedrone_params_speed_movement = "freedrone_params_speed_movement";

	public static readonly Key freedrone_params_movement_damping = "freedrone_params_movement_damping";

	public static readonly Key freedrone_params_speed_multiplier = "freedrone_params_speed_multiplier";

	public static readonly Key freedrone_params_max_speed = "freedrone_params_max_speed";

	public static readonly Key freedrone_params_speed_damping = "freedrone_params_speed_damping";

	public static readonly Key freedrone_params_speed_rotation = "freedrone_params_speed_rotation";

	public static readonly Key freedrone_params_target_damping = "freedrone_params_target_damping";

	public static readonly Key freedrone_params_rotation_damping = "freedrone_params_rotation_damping";

	public static readonly Key freedrone_params_lockhead_time = "freedrone_params_lockhead_time";

	public static readonly Key freedrone_params_fov = "freedrone_params_fov";

	public static readonly Key freedrone_params_frequency_gain = "freedrone_params_frequency_gain";

	public static readonly Key freedrone_params_amplitude_gain = "freedrone_params_amplitude_gain";

	public static readonly Key content_available_before_complete_event = "content_available_before_complete_event";

	public static readonly Key halloweenEventTracksCount = "helloween_event_tracks_count";

	public static readonly Key halloweenEventPumpkinsCount = "helloween_event_pumpkins_count";

	public static readonly Key halloweenCompleteDescription = "helloween_complete_description";

	public static readonly Key no_dlc_vinyl_apply = "no_dlc_vinyl_apply";

	public static readonly Key no_dlc_livery_apply = "no_dlc_livery_apply";

	public static readonly Key settings_sound_target_car_volume = "settings_sound_target_car_volume";

	public static readonly Key settings_sound_other_cars_volume = "settings_sound_other_cars_volume";

	public static readonly Key settings_sound_seq_transmission_volume = "settings_sound_seq_transmission_volume";

	public static readonly Key settings_sound_ambience_volume = "settings_sound_ambience_volume";

	public static readonly Key vote_need_buy_dlc_track = "vote_need_buy_dlc_track";

	public static readonly Key vote_only_admin = "vote_only_admin";

	public static readonly Key wrong_mask = "wrong_mask";

	public static readonly Key need_bind_ignition_starter = "need_bind_ignition_starter";

	public static readonly Key empty_bind_ignition_starter = "empty_bind_ignition_starter";

	public static readonly Key vinyls_sort_layer = "vinyls_sort_layer";

	public static readonly Key symmetry = "symmetry";

	public static readonly Key move = "move";

	public static readonly Key rotate = "rotate";

	public static readonly Key scale = "scale";

	public static readonly Key stretch = "stretch";

	public static readonly Key flip = "flip";

	public static readonly Key projection = "projection";

	public static readonly Key groupmask = "groupmask";

	public static readonly Key editor_hide_hints = "editor_hide_hints";

	public static readonly Key editor_show_hints = "editor_show_hints";

	public static readonly Key visualise_map_legend = "visualise_map_legend";

	public static readonly Key top32_champ_fail_connect = "top32_champ_fail_connect";

	public static readonly Key top32_screen_title_net_race = "top32_screen_title_net_race";

	public static readonly Key top32_screen_title_view_replay = "top32_screen_title_view_replay";

	public static readonly Key top32_menu_title = "top32_menu_title";

	public static readonly Key top32_parallel_grid = "top32_parallel_grid";

	public static readonly Key top32_consistent_grid = "top32_consistent_grid";

	public static readonly Key top32_rules_xds = "top32_rules_xds";

	public static readonly Key top32_rules_dp = "top32_rules_dp";

	public static readonly Key top32_stage = "top32_stage";

	public static readonly Key top32_select_champ_title = "top32_select_champ_title";

	public static readonly Key top32_empty_list = "top32_empty_list";

	public static readonly Key top32_players_list = "top32_players_list";

	public static readonly Key top32_to_start = "top32_to_start";

	public static readonly Key top32_qualification_started = "top32_qualification_started";

	public static readonly Key top32_qualification_ended = "top32_qualification_ended";

	public static readonly Key top32_begin_qualification = "top32_begin_qualification";

	public static readonly Key top32_attempt = "top32_attempt";

	public static readonly Key top32_next_attempt = "top32_next_attempt";

	public static readonly Key top32_best_result = "top32_best_result";

	public static readonly Key top32_start_race = "top32_start_race";

	public static readonly Key top32_finish_training = "top32_finish_training";

	public static readonly Key top32_finish_qualification = "top32_finish_qualification";

	public static readonly Key top32_race_starts_in_sec = "top32_race_starts_in_sec";

	public static readonly Key top32_returning_to_champ_screen = "top32_returning_to_champ_screen";

	public static readonly Key top32_player_status = "top32_player_status";

	public static readonly Key top32_status_spectator = "top32_status_spectator";

	public static readonly Key top32_status_participant = "top32_status_participant";

	public static readonly Key top32_applications = "top32_applications";

	public static readonly Key top32_champ_car = "top32_champ_car";

	public static readonly Key top32_rules_header = "top32_rules_header";

	public static readonly Key top32_join = "top32_join";

	public static readonly Key top32_change_car = "top32_change_car";

	public static readonly Key top32_qualification = "top32_qualification";

	public static readonly Key top32_race = "top32_race";

	public static readonly Key top32_show_full_grid = "top32_show_full_grid";

	public static readonly Key top32_final = "top32_final";

	public static readonly Key top32_evaluation_xds = "top32_evaluation_xds";

	public static readonly Key top32_evaluation_dp = "top32_evaluation_dp";

	public static readonly Key top32_point_to_qualification = "top32_point_to_qualification";

	public static readonly Key top32_stage_changed = "top32_stage_changed";

	public static readonly Key top32_stage_pause_started = "top32_stage_pause_started";

	public static readonly Key top32_race_time_ended = "top32_race_time_ended";

	public static readonly Key top32_reward = "top32_reward";

	public static readonly Key top32_get_place = "top32_get_place";

	public static readonly Key top32_disqualification = "top32_disqualification";

	public static readonly Key top32_lose_message = "top32_lose_message";

	public static readonly Key top32_low_points = "top32_low_points";

	public static readonly Key top32_low_players = "top32_low_players";

	public static readonly Key top32_champ_closed = "top32_champ_closed";

	public static readonly Key top32_stage_change_time = "top32_stage_change_time";

	public static readonly Key top32_champ_start_timer = "top32_champ_start_timer";

	public static readonly Key top32_not_registered = "top32_not_registered";

	public static readonly Key top32_registration_success = "top32_registration_success";

	public static readonly Key top32_registration_fail = "top32_registration_fail";

	public static readonly Key top32_summarize = "top32_summarize";

	public static readonly Key top32_bad_connection = "top32_bad_connection";

	public static readonly Key top32_bad_connection_warning = "top32_bad_connection_warning";

	public static readonly Key top32_unable_to_proceed_with_adh = "top32_unable_to_proceed_with_adh";

	public static readonly Key top32_chosen_champs_car_and_profile = "top32_chosen_champs_car_and_profile";

	public static readonly Key top32_disabled_on_mono = "top32_disabled_on_mono";

	public static readonly Key top32_by_qualification = "top32_by_qualification";

	public static readonly Key top32_different_setup_registration = "top32_different_setup_registration";

	public static readonly Key top32_room_connect_success = "top32_room_connect_success";

	public static readonly Key top32_room_connect_fail = "top32_room_connect_fail";

	public static readonly Key top32_change_adh_to_continue = "top32_change_adh_to_continue";

	public static readonly Key top32_leave_champ = "top32_leave_champ";

	public static readonly Key top32_leave_champ_question = "top32_leave_champ_question";

	public static readonly Key top32_champ_history = "top32_champ_history";

	public static readonly Key top32_loading_history_error = "top32_loading_history_error";

	public static readonly Key top32_empty_history_champs = "top32_empty_history_champs";

	public static readonly Key top32_workshop_bodykits_restricted = "top32_workshop_bodykits_restricted";

	public static readonly Key telemetry = "telemetry";

	public static readonly Key load_ghost_error = "load_ghost_error";

	public static readonly Key view_ghost_leader_replay = "view_ghost_leader_replay";

	public static readonly Key view_ghost_chaser_replay = "view_ghost_chaser_replay";

	public static readonly Key modifications_customs_empty_list = "modifications_customs_empty_list";

	public static readonly Key liveries_empty_list = "liveries_empty_list";

	public static readonly Key tracks_empty_list = "tracks_empty_list";

	public static readonly Key cars_empty_list = "cars_empty_list";

	public static readonly Key generic_copy = "generic_copy";

	public static readonly Key liveries_publish_private = "liveries_publish_private";

	public static readonly Key swaps_unavailable = "swaps_unavailable";

	public static readonly Key save_custom_setup = "save_custom_setup";

	public static readonly Key save_custom_setup_question = "save_custom_setup_question";

	public static readonly Key save_server_livery_question = "save_server_livery_question";

	public static readonly Key custom_setup_not_found = "custom_setup_not_found";

	public static readonly Key error_save_not_full_livery = "error_save_not_full_livery";

	public static readonly Key save_server_cropped_livery_question = "save_server_cropped_livery_question";

	public static readonly Key save_server_cropped_livery_question_console = "save_server_cropped_livery_question_console";

	public static readonly Key rewrite_livery_question = "rewrite_livery_question";

	public static readonly Key rewrite_custom_setup_question = "rewrite_custom_setup_question";

	public static readonly Key custom_setup_success_save = "custom_setup_success_save";

	public static readonly Key livery_success_save = "livery_success_save";

	public static readonly Key livery_rewrite_save = "livery_rewrite_save";

	public static readonly Key custom_setup_fail_save = "custom_setup_fail_save";

	public static readonly Key custom_setup_rewrite_save = "custom_setup_rewrite_save";

	public static readonly Key custom_setup_slots_exceed = "custom_setup_slots_exceed";

	public static readonly Key liveries_slots_exceed = "liveries_slots_exceed";

	public static readonly Key livery_loading = "livery_loading";

	public static readonly Key custom_setup_delete_promt = "custom_setup_delete_promt";

	public static readonly Key custom_setup_save_promt = "custom_setup_save_promt";

	public static readonly Key publish_setup_question = "publish_setup_question";

	public static readonly Key publish_livery_question = "publish_livery_question";

	public static readonly Key publish_setup_success = "publish_setup_success";

	public static readonly Key publish_livery_success = "publish_livery_success";

	public static readonly Key setup_already_exist = "setup_already_exist";

	public static readonly Key publish = "publish";

	public static readonly Key actions = "actions";

	public static readonly Key rate = "rate";

	public static readonly Key setup_rate_success = "setup_rate_success";

	public static readonly Key setup_rate_updated = "setup_rate_updated";

	public static readonly Key livery_rate_success = "livery_rate_success";

	public static readonly Key livery_rate_updated = "livery_rate_updated";

	public static readonly Key wrong_publication_code = "wrong_publication_code";

	public static readonly Key rating = "rating";

	public static readonly Key subscribe_to_news_step1_name = "subscribe_to_news_step1_name";

	public static readonly Key official_setups = "official_setups";

	public static readonly Key official_liveries = "official_liveries";

	public static readonly Key my_uploaded_setups = "my_uploaded_setups";

	public static readonly Key already_published = "already_published";

	public static readonly Key sharing_rate_dialog_title = "sharing_rate_dialog_title";

	public static readonly Key sharing_rate_livery_dialog_title = "sharing_rate_livery_dialog_title";

	public static readonly Key sharing_server_items = "sharing_server_items";

	public static readonly Key sharing_menu_title = "sharing_menu_title";

	public static readonly Key categories = "categories";

	public static readonly Key layers_count = "layers_count";

	public static readonly Key download_count = "download_count";

	public static readonly Key upload_date = "upload_date";

	public static readonly Key creator = "creator";

	public static readonly Key publication_code = "publication_code";

	public static readonly Key publication_not_exist = "publication_not_exist";

	public static readonly Key reached_max_publication_count_per_day = "reached_max_publication_count_per_day";

	public static readonly Key publish_restricted = "publish_restricted";

	public static readonly Key naming = "naming";

	public static readonly Key input_publication_code = "input_publication_code";

	public static readonly Key input_track_name = "input_track_name";

	public static readonly Key input_bodykit_name = "input_bodykit_name";

	public static readonly Key input_author_name = "input_author_name";

	public static readonly Key input_livery_name = "input_livery_name";

	public static readonly Key publish_livery_description = "publish_livery_description";

	public static readonly Key publish_livery_no_tag_error = "publish_livery_no_tag_error";

	public static readonly Key wrong_name = "wrong_name";

	public static readonly Key expand_ui_list = "expand_ui_list";

	public static readonly Key shrink_ui_list = "shrink_ui_list";

	public static readonly Key show_author_liveries = "show_author_liveries";

	public static readonly Key search_result = "search_result";

	public static readonly Key new_params = "new_params";

	public static readonly Key current_params = "current_params";

	public static readonly Key filters = "filters";

	public static readonly Key active_filter = "active_filter";

	public static readonly Key sharing_setups_unavailable = "sharing_setups_unavailable";

	public static readonly Key toggle_sharing_params_view = "toggle_sharing_params_view";

	public static readonly Key crossplatform_publications = "crossplatform_publications";

	public static readonly MultyKey region_names = new MultyKey("region_names").Add(new string[5] { "america", "europe", "japan", "asia", "south_america" });

	public static readonly MultyKey top32_stage_ = new MultyKey("top32_stage_").Add(new string[10] { "top32_stage_closed", "top32_stage_registration", "top32_stage_qualification", "top32_stage_top32", "top32_stage_top16", "top32_stage_top8", "top32_stage_top4", "top32_stage_final", "top32_stage_loser", "top32_stage_ended" });

	public static readonly MultyKey lvlup_ = new MultyKey("lvlup_").Add(typeof(GameGlobalFeature), (string v) => "lvlup_" + v.ToLower()).Remove(new string[1] { "lvlup_unknown" });

	public static readonly MultyKey xds_rsn_ = new MultyKey("xds_rsn_").Add(typeof(DisqualifyReason), (string v) => "xds_rsn_" + v.ToLower()).Remove("xds_rsn_unknown");

	public static readonly MultyKey xds_room_ = new MultyKey("xds_room_").Add(typeof(SubroomFlags), (string v) => "xds_room_" + v.ToLower()).Remove(new string[3] { "xds_room_none", "xds_room_xdscollide", "xds_room_xdsrecord" });

	public static readonly MultyKey mp_league_ = new MultyKey("mp_league_").Add(1, 5, "mp_league_{0}");

	public static readonly MultyKey network_room_mode_ = new MultyKey("network_room_mode_").Add(typeof(SubroomMode), (string v) => "network_room_mode_" + v.ToLower()).Remove("network_room_mode_showtrack");

	public static readonly MultyKey input_action_ = new MultyKey("input_action_").Add((ActionType v) => (GameInput.Action.GetAttributeOfType<SystemActionAttribute>(v) != null) ? null : ("input_action_" + v.ToString().ToLower())).Add(new string[8] { "input_action_turnl", "input_action_turnr", "input_action_dpaddown", "input_action_dpadleft", "input_action_dpadright", "input_action_dpadup", "input_action_interfaceselect", "input_action_sync_headlights" }).Remove(new string[1] { "input_action_erroraction" });

	public static readonly MultyKey settings_category__title = new MultyKey("settings_category_{0}_title").Add(new string[4] { "game", "steer", "graphics", "sounds" }, (string v) => "settings_category_" + v + "_title");

	public static readonly MultyKey settings_category__description = new MultyKey("settings_category_{0}_description").Add(new string[4] { "game", "steer", "graphics", "sounds" }, (string v) => "settings_category_" + v + "_description");

	public static readonly MultyKey room_desc_ = new MultyKey("room_desc_").Add(typeof(SubroomMode), (string v) => "room_desc_" + v.ToLower()).Remove("room_desc_showtrack");

	public static readonly MultyKey globalrating_subleague_ = new MultyKey("globalrating_subleague_").Add(1, 3, "globalrating_subleague_{0}");

	public static readonly MultyKey globalrating_league_ = new MultyKey("globalrating_league_").Add(1, 4, "globalrating_league_{0}");

	public static readonly MultyKey cht_popup_ = new MultyKey("cht_popup_").Add(new string[4] { "ignore", "overlayprofile", "private", "unignore" }, (string v) => "cht_popup_" + v);

	public static readonly MultyKey carpart_ = new MultyKey("carpart_").Add(new string[2] { "carpart_chassis", "carpart_wheel" });

	public static readonly MultyKey track_ = new MultyKey("track_").Add(new string[16]
	{
		"track_1_name", "track_11_name", "track_15_name", "track_17_name", "track_19_name", "track_2_name", "track_25_name", "track_ebisu_name", "track_petersburg_name", "track_redring_name",
		"track_redring_winter_name", "track_redrock_name", "track_23_name", "track_irwindale_name", "track_atron_name", "track_pacifichills_name"
	});

	public static readonly MultyKey new_achievement_ = new MultyKey("new_achievement_").Add(0, 36, "new_achievement_1_{0}_name").Remove("new_achievement_1_23_name").Add(0, 36, "new_achievement_1_{0}_desc")
		.Remove("new_achievement_1_23_desc")
		.Add(new string[2] { "new_achievement_platinum_name", "new_achievement_platinum_desc" });

	public static readonly MultyKey network_notif_create_vote_ = new MultyKey("network_notif_create_vote_").Add(typeof(VoteType), (string v) => "network_notif_create_vote_" + v.ToLower());

	public static readonly Key trophy_set_details = "trophy_set_details";

	public static readonly Key NotAvailable = "not_available";

	public static readonly Key Unknown = "unknown";

	public static readonly Key colorpicker_materials_title_chameleon = "colorpicker_materials_title_chameleon";

	public static readonly Key colorpicker_materials_title_holographic = "colorpicker_materials_title_holographic";

	public static readonly Key colorpicker_materials_title_base = "colorpicker_materials_title_base";

	public static readonly Key colorpicker_materials_title_chrome = "colorpicker_materials_title_chrome";

	public static readonly Key colorpicker_materials_title_matte = "colorpicker_materials_title_matte";

	public static readonly Key colorpicker_materials_title_metalic = "colorpicker_materials_title_metalic";

	public static readonly Key colorpicker_materials_title_candy = "colorpicker_materials_title_candy";

	public static readonly Key colorpicker_materials_title_satin = "colorpicker_materials_title_satin";

	public static readonly Key colorpicker_materials_title_solid = "colorpicker_materials_title_solid";

	public static readonly Key network_notif_create_vote_map = "network_notif_create_vote_map";

	public static readonly Key network_notif_create_vote_kick = "network_notif_create_vote_kick";

	public static readonly Key network_notif_create_vote_ban = "network_notif_create_vote_ban";

	public static readonly Key network_notif_create_vote_night = "network_notif_create_vote_night";

	public static readonly Key driftevent_failx1 = "driftevent_failx1";

	public static readonly Key driftevent_speed_x1 = "driftevent_speed_x1";

	public static readonly Key driftevent_speed_x2 = "driftevent_speed_x2";

	public static readonly Key driftevent_sidechange_x1 = "driftevent_sidechange_x1";

	public static readonly Key driftevent_sidechange_x2 = "driftevent_sidechange_x2";

	public static readonly Key driftevent_angle_x1 = "driftevent_angle_x1";

	public static readonly Key driftevent_angle_x2 = "driftevent_angle_x2";

	public static readonly Key driftevent_accuracy_x1 = "driftevent_accuracy_x1";

	public static readonly Key driftevent_accuracy_x2 = "driftevent_accuracy_x2";

	public static readonly Key driftevent_quality_x1 = "driftevent_quality_x1";

	public static readonly Key driftevent_quality_x2 = "driftevent_quality_x2";

	public static readonly Key driftevent_jump_x1 = "driftevent_jump_x1";

	public static readonly Key driftevent_jump_x2 = "driftevent_jump_x2";

	public static readonly Key driftevent_backward_x1 = "driftevent_backward_x1";

	public static readonly Key driftevent_backward_x2 = "driftevent_backward_x2";

	public static readonly Key driftevent_clippingzone_x1 = "driftevent_clippingzone_x1";

	public static readonly Key driftevent_clippingzone_x2 = "driftevent_clippingzone_x2";

	public static readonly Key driftevent_clippingpoint_x1 = "driftevent_clippingpoint_x1";

	public static readonly Key driftevent_clippingpoint_x2 = "driftevent_clippingpoint_x2";

	public static readonly Key driftevent_clippingpoint_x3 = "driftevent_clippingpoint_x3";

	public static readonly Key driftevent_clippingpoint_x4 = "driftevent_clippingpoint_x4";

	public static readonly Key driftevent_customClippingEvent_x1 = "driftevent_custom_clipping_event_x1";

	public static readonly Key driftevent_customClippingEvent_x2 = "driftevent_custom_clipping_event_x2";

	public static readonly Key driftevent_kissTheWall_x1 = "driftevent_kissthewall_x1";

	public static readonly Key customization_cameras = "customization_cameras";

	public static readonly Key customization_cameras_save_changes_text = "customization_cameras_save_changes_text";

	public static readonly Key local_camera_settings = "local_camera_settings";

	public static readonly Key camera_mode = "camera_mode";

	public static readonly Key camera_fov = "camera_fov";

	public static readonly Key camera_height = "camera_height";

	public static readonly Key camera_tilt = "camera_tilt";

	public static readonly Key camera_point_center = "camera_point_center";

	public static readonly Key camera_point_free = "camera_point_free";

	public static readonly Key camera_point_back = "camera_point_back";

	public static readonly Key camera_point_unfocus = "camera_point_unfocus";

	public static readonly Key camera_additional_settings = "camera_additional_settings";

	public static readonly Key view_camera_gate = "view_camera_gate";

	public static readonly Key view_camera_focus = "view_camera_focus";

	public static readonly Key view_camera_exposition = "view_camera_exposition";

	public static readonly Key view_camera_aperture = "view_camera_aperture";

	public static readonly Key view_camera_chromatic_aberration = "view_camera_chromatic_aberration";

	public static readonly Key view_camera_сontrast = "view_camera_сontrast";

	public static readonly Key view_camera_saturation = "view_camera_saturation";

	public static readonly Key view_camera_brightness = "view_camera_brightness";

	public static readonly Key view_camera_vignetting = "view_camera_vignetting";

	public static readonly Key view_camera_temperature = "view_camera_temperature";

	public static readonly Key view_camera_pilot = "view_camera_pilot";

	public static readonly Key camera_customizer_offset_y = "camera_customizer_offset_y";

	public static readonly Key camera_customizer_offset_x = "camera_customizer_offset_x";

	public static readonly Key camera_customizer_offset_z = "camera_customizer_offset_z";

	public static readonly Key camera_customizer_pitch_angle = "camera_customizer_pitch_angle";

	public static readonly Key global_camera_settings = "global_camera_settings";

	public static readonly Key cockpit_camera = "cockpit_camera";

	public static readonly Key cockpit_camera_settings = "cockpit_camera_settings";

	public static readonly Key cockpit_camera_aim_mode = "cockpit_camera_aim_mode";

	public static readonly Key cockpit_camera_aim_mode_velocity = "cockpit_camera_aim_mode_velocity";

	public static readonly Key cockpit_camera_aim_mode_steer = "cockpit_camera_aim_mode_steer";

	public static readonly Key cockpit_camera_max_rotation_speed = "cockpit_camera_max_rotation_speed";

	public static readonly Key cockpit_camera_max_rotation_angle = "cockpit_camera_max_rotation_angle";

	public static readonly Key cockpit_camera_velocity_scale = "cockpit_camera_velocity_scale";

	public static readonly Key cockpit_camera_smooth = "cockpit_camera_smooth";

	public static readonly Key cockpit_camera_accel_toggle = "cockpit_camera_accel_toggle";

	public static readonly Key cockpit_camera_accel_smooth = "cockpit_camera_accel_smooth";

	public static readonly Key cockpit_camera_deadzone = "cockpit_camera_deadzone";

	public static readonly Key free_camera_button = "free_camera_button";

	public static readonly Key settings_input_stabilization = "settings_input_stabilization";

	public static readonly Key settings_input_move_mouse_axis = "settings_input_move_mouse_axis";

	public static readonly Key settings_input_move_mouse_axis_desc = "settings_input_move_mouse_axis_desc";

	public static readonly Key settings_input_move_mouse_axis_on_delta = "settings_input_move_mouse_axis_on_delta";

	public static readonly Key settings_input_move_mouse_axis_on_coord = "settings_input_move_mouse_axis_on_coord";

	public static readonly Key settings_input_prediction = "settings_input_prediction";

	public static readonly Key settings_input_inertia = "settings_input_inertia";

	public static readonly Key settings_input_acceleration = "settings_input_acceleration";

	public static readonly Key settings_input_smoothing = "settings_input_smoothing";

	public static readonly Key settings_input_postprocess = "settings_input_postprocess";

	public static readonly Key settings_input_postprocess_x = "settings_input_postprocess_x";

	public static readonly Key settings_input_postprocess_y = "settings_input_postprocess_y";

	public static readonly Key settings_input_stabilization_desc = "settings_input_stabilization_desc";

	public static readonly Key settings_input_prediction_desc = "settings_input_prediction_desc";

	public static readonly Key settings_input_inertia_desc = "settings_input_inertia_desc";

	public static readonly Key settings_input_acceleration_desc = "settings_input_acceleration_desc";

	public static readonly Key settings_input_smoothing_desc = "settings_input_smoothing_desc";

	public static readonly Key settings_input_postprocess_desc = "settings_input_postprocess_desc";

	public static readonly Key settings_input_preset_type = "settings_input_preset_type";

	public static readonly Key settings_input_preset_custom = "settings_input_preset_custom";

	public static readonly Key settings_input_preset_beginner = "settings_input_preset_beginner";

	public static readonly Key settings_input_preset_pro = "settings_input_preset_pro";

	public static readonly Key settings_input_recommended_value = "settings_input_recommended_value";

	public static readonly Key settings_input_activate_ffb = "settings_input_activate_ffb";

	public static readonly Key settings_input_deactivate_ffb = "settings_input_deactivate_ffb";

	public static readonly Key need_set_active_ffb_device = "need_set_active_ffb_device";

	public static readonly Key mods_info_godvoice = "mods_info_godvoice";

	public static readonly Key engine_swap_category_all = "engine_swap_category_all";

	public static readonly Key engine_swap_category_opposite = "engine_swap_category_opposite";

	public static readonly Key engine_swap_category_rotary = "engine_swap_category_rotary";

	public static readonly Key engine_swap_category_other = "engine_swap_category_other";

	public static readonly Key engine_swap_quest_progress_label = "engine_swap_quest_progress_label";

	public static readonly Key eula_age_confirm_title = "eula_age_confirm_title";

	public static readonly Key eula_age_confirm_main = "eula_age_confirm_main";

	public static readonly Key eula_parents_confirm_main = "eula_parents_confirm_main";

	public static readonly Key eula_parent_confirm_main_block = "eula_parent_confirm_main_block";

	public static readonly Key eula_licence_agreegment_title = "eula_licence_agreegment_title";

	public static readonly Key eula_licence_agreegment_main = "eula_licence_agreegment_main";

	public static readonly Key eula_privacy_policy_title = "eula_privacy_policy_title";

	public static readonly Key eula_privacy_policy_main = "eula_privacy_policy_main";

	public static readonly Key eula_privacy_policy_main_block = "eula_privacy_policy_main_block";

	public static readonly Key refuse = "refuse";

	public static readonly Key accept = "accept";

	public static readonly Key agreement = "agreement";

	public static readonly Key swaps_mode_swaps = "swaps_mode_swaps";

	public static readonly Key swaps_mode_setups = "swaps_mode_setups";

	public static readonly Key swaps_install_button = "swaps_install_button";

	public static readonly Key swaps_rev_engine_button = "swaps_rev_engine_button";

	public static readonly Key swaps_engine_parts_required = "swaps_engine_parts_required";

	public static readonly Key swaps_new_item_added_header = "swaps_new_item_added_header";

	public static readonly Key swaps_not_enough_parts_template = "swaps_not_enough_parts_template";

	public static readonly Key swaps_unlock_message_one_of_template = "swaps_unlock_message_one_of_template";

	public static readonly Key swaps_unlock_message_all_template = "swaps_unlock_message_all_template";

	public static readonly Key reward = "reward";

	public static readonly Key reward_received = "reward_received";

	public static readonly Key get_reward = "get_reward";

	public static readonly Key xds_ui_race_reward = "xds_ui_race_reward";

	public static readonly Key slow_zones = "slow_zones";

	public static readonly Key vr_mode_title = "vr_mode_title";

	public static readonly Key vr_put_on_head_device = "vr_put_on_head_device";

	public static readonly Key vr_check_connect = "vr_check_connect";

	public static readonly Key vr_section_not_available = "vr_section_not_available";

	public static readonly Key vr_reset_position_and_rotation = "vr_reset_position_and_rotation";

	public static readonly Key vr_reset_bind_godvoice = "vr_reset_bind_godvoice";

	public static readonly Key vrcamreset = "vrcamreset";

	public static readonly Key car_catalog_category_favorites = "car_catalog_category_favorites";

	public static readonly Key car_catalog_category_all = "car_catalog_category_all";

	public static readonly Key car_catalog_vendors_all = "car_catalog_vendors_all";

	public static readonly Key car_catalog_add_to_favorites = "car_catalog_add_to_favorites";

	public static readonly Key car_catalog_remove_from_favorites = "car_catalog_remove_from_favorites";

	public static readonly Key car_catalog_favorites_empty = "car_catalog_favorites_empty";

	public static readonly Key player_profile_cars = "player_profile_cars";

	public static readonly Key ugc_browser_select_service_provider_title = "ugc_browser_select_service_provider_title";

	public static readonly Key ugc_browser_launch_track = "ugc_browser_launch_track";

	public static readonly Key ugc_browser_vote_result_success = "ugc_browser_vote_result_success";

	public static readonly Key ugc_browser_vote_result_error = "ugc_browser_vote_result_error";

	public static readonly Key ugc_browser_subscribe = "ugc_browser_subscribe";

	public static readonly Key ugc_browser_unsubscribe = "ugc_browser_unsubscribe";

	public static readonly Key ugc_browser_install_insufficient_storage_space = "ugc_browser_install_insufficient_storage_space";

	public static readonly Key ugc_browser_avaiable_storage_space = "ugc_browser_avaiable_storage_space";

	public static readonly Key ugc_manager_authentication_email_title = "ugc_manager_authentication_email_title";

	public static readonly Key ugc_manager_authentication_email_input_field = "ugc_manager_authentication_email_input_field";

	public static readonly Key ugc_manager_authentication_submit_code_title = "ugc_manager_authentication_submit_code_title";

	public static readonly Key ugc_manager_authentication_submit_code_input_field = "ugc_manager_authentication_submit_code_input_field";

	public static readonly Key ugc_manager_subscribe_failed = "ugc_manager_subscribe_failed";

	public static readonly Key ugc_manager_unsubscribe_failed = "ugc_manager_unsubscribe_failed";

	public static readonly Key ugc_manager_authentication_email_error = "ugc_manager_authentication_email_error";

	public static readonly Key ugc_manager_authentication_email_submit_code_error = "ugc_manager_authentication_email_submit_code_error";

	public static readonly Key ugc_manager_show_item_in_workshop = "ugc_manager_show_item_in_workshop";

	public static readonly Key ugc_manager_general_authentication_error = "ugc_manager_general_authentication_error";

	public static readonly Key ugc_manager_failed_to_get_terms_of_use = "ugc_manager_failed_to_get_terms_of_use";

	public static readonly Key ugc_manager_failed_to_install_item = "ugc_manager_failed_to_install_item";

	public static readonly Key ugc_manager_is_storage_space_insufficient = "ugc_manager_is_storage_space_insufficient";

	public static readonly Key ugc_manager_is_downloaded_prepare_to_install = "ugc_manager_is_downloaded_prepare_to_install";

	public static readonly Key ugc_manager_download_failed = "ugc_manager_download_failed";

	public static readonly Key ugc_manager_is_not_ready = "ugc_manager_is_not_ready";

	public static readonly Key ugc_manager_log_out_mod_io = "ugc_manager_log_out_mod_io";

	public static readonly Key ugc_manager_params_label = "ugc_manager_params_label";

	public static readonly Key car_slots_caption = "car_slots_caption";

	public static readonly Key car_slots_create_new = "car_slots_create_new";

	public static readonly Key car_slots_write_caption = "car_slots_write_caption";

	public static readonly Key car_slots_rename = "car_slots_rename";

	public static readonly Key car_slots_copy = "car_slots_copy";

	public static readonly Key car_slots_delete = "car_slots_delete";

	public static readonly Key car_slots_change = "car_slots_change";

	public static readonly Key car_slots_custom_setup_already_in_use_delete_warning = "car_slots_custom_setup_already_in_use_delete_warning";

	public static readonly Key car_slots_livery_already_in_use_delete_warning = "car_slots_livery_already_in_use_delete_warning";

	public static readonly Key car_slots_livery_already_in_use_edit_warning = "car_slots_livery_already_in_use_edit_warning";

	public static readonly Key car_slots_dont_remind = "car_slots_dont_remind";

	public static readonly Key car_slots_count = "car_slots_count";

	public static readonly Key car_slots_exceed_error = "car_slots_exceed_error";

	public static readonly Key car_slots_last_delete_error = "car_slots_last_delete_error";

	public static readonly Key car_slots_empty_caption_error = "car_slots_empty_caption_error";

	public static readonly Key car_slot_locked = "car_slot_locked";

	public static readonly Key car_slot_locked_console = "car_slot_locked_console";

	public static readonly Key car_slot_task = "car_slot_task";

	public static readonly Key car_slot_reward = "car_slot_reward";

	public static readonly Key car_slot_or = "car_slot_or";

	public static readonly Key car_slot_unlock_previous = "car_slot_unlock_previous";

	public static readonly Key car_slot_2_task_1 = "car_slot_2_task_1";

	public static readonly Key car_slot_2_task_2 = "car_slot_2_task_2";

	public static readonly Key car_slot_3_task_1 = "car_slot_3_task_1";

	public static readonly Key car_slot_3_task_2 = "car_slot_3_task_2";

	public static readonly Key car_slot_4_task_1 = "car_slot_4_task_1";

	public static readonly Key car_slot_5_task_1 = "car_slot_5_task_1";

	public static readonly Key car_slot_5_task_2 = "car_slot_5_task_2";

	public static readonly Key need_set_properly_steering_device_in_input_settings = "need_set_properly_steering_device_in_input_settings";

	public static readonly Key quests_window_title = "quests_window_title";

	public static readonly Key quests_list_empty = "quests_list_empty";

	public static readonly Key quests_unavailable = "quests_unavailable";

	public static readonly Key quest_until_the_end = "quest_until_the_end";

	public static readonly Key quest_complete = "quest_complete";

	public static readonly Key game_mode = "game_mode";

	public static readonly Key race_mode = "race_mode";

	public static readonly Key any = "any";

	public static readonly Key remains_to_repeat = "remains_to_repeat";

	public static readonly Key tracks = "tracks";

	public static readonly Key engines = "engines";

	public static readonly Key setup = "setup";

	public static readonly Key cup = "cup";

	public static readonly Key cups = "cups";

	public static readonly Key configs = "configs";

	public static readonly Key minimum_count_players = "minimum_count_players";

	public static readonly Key minimum_count_friend = "minimum_count_friend";

	public static readonly Key complete = "complete";

	public static readonly Key win = "win";

	public static readonly Key earn = "earn";

	public static readonly Key accumulate = "accumulate";

	public static readonly Key get = "get";

	public static readonly Key xds_points = "xds_points";

	public static readonly Key experience = "experience";

	public static readonly Key for_leader = "for_leader";

	public static readonly Key for_chaser = "for_chaser";

	public static readonly Key minimum_position = "minimum_position";

	public static readonly Key alternative_reward_available = "alternative_reward_available";

	public static readonly Key need_set_main_branch = "need_set_main_branch";

	public static readonly Key vinyl = "vinyl";

	public static readonly Key without_livery = "without_livery";

	public static readonly Key currency = "currency";

	public static readonly Key quest_restriction = "quest_restriction";

	public static readonly Key available_only_in_quests = "available_only_in_quests";

	public static readonly Key reward_for = "reward_for";

	public static readonly Key for_participate = "for_participate";

	public static readonly Key nav_download = "nav_download";

	public static readonly Key downloading_ugc_item = "downloading_ugc_item";

	public static readonly Key ugc_item_downloaded = "ugc_item_downloaded";

	public static readonly Key do_you_want_download_ugc_map = "do_you_want_download_ugc_map";

	public static readonly Key external_tracks = "external_tracks";

	public static readonly Key standard_tracks = "standard_tracks";

	public static readonly Key ugc_track_downloaded_info = "ugc_track_downloaded_info";

	public static readonly Key ugc_track_meta_error = "ugc_track_meta_error";

	public static readonly Key show_in_steam_overlay = "show_in_steam_overlay";

	public static readonly Key freecam_mode_title_description = "freecam_mode_title_description";

	public static readonly Key empty_slot = "empty_slot";

	public static readonly Key network_game_mapname = "network_game_mapname";

	public static readonly Key external_track_access_denied = "external_track_access_denied";

	public static readonly Key unpacking_downloaded_external_resources = "unpacking_downloaded_external_resources";

	public static readonly Key front_suspension = "front_suspension";

	public static readonly Key rear_suspension = "rear_suspension";

	public static readonly MultyKey carProfileParameterCaptionDyno = new MultyKey("car_profile_prm_caption_dyno_{0}").Add(new string[67]
	{
		"brake_front_percent", "final_drive", "front_ackerman", "front_base_offset", "front_camber", "front_caster", "front_disc_diam", "front_lock", "front_spring_stiffness", "front_spring_way",
		"front_stabilizer", "front_tire_type", "front_toe", "front_tyre_pressure", "front_tyre_profile", "front_tyre_width", "gearbox1", "gearbox2", "gearbox3", "gearbox4",
		"gearbox5", "gearbox6", "gearbox7", "gearbox8", "max_brake_torque", "max_torque", "max_torque_rpm", "rear_base_offset", "rear_camber", "rear_disc_diam",
		"rear_spring_stiffness", "rear_spring_way", "rear_stabilizer", "front_fast_bump", "front_fast_rebound", "front_slow_bump", "front_slow_rebound", "rear_fast_bump", "rear_fast_rebound", "rear_slow_bump",
		"rear_slow_rebound", "rear_tire_type", "rear_toe", "rear_tyre_pressure", "rear_tyre_profile", "rear_tyre_width", "rev_limiter", "turbo_charged", "turbo_pressure", "brake_abs",
		"traction_control", "launch_control", "seq_transmision_sound", "wastegate_sound", "diff_type", "diff_lock_coef", "diff_preload", "diff_lock_power", "diff_lock_coast", "front_kingpin",
		"front_wheel_center_trail", "drivetrain_type", "mass", "weight_front_percent", "weight_up_percent", "front_tire_stretch", "rear_tire_stretch"
	}, (string v) => "car_profile_prm_caption_dyno_" + v);
}
