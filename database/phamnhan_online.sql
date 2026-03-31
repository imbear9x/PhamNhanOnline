--
-- PostgreSQL database dump
--

\restrict lfv5QTJmP6RrK1GA6jzzEg7DuzvjutlNL58PWRb83Vb3T8OciBqxtEbs4X2n5iL

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: account_credentials; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.account_credentials (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    account_id uuid NOT NULL,
    provider character varying(20) NOT NULL,
    provider_user_id character varying(255) NOT NULL,
    password_hash text,
    created_at timestamp without time zone DEFAULT now()
);


ALTER TABLE public.account_credentials OWNER TO postgres;

--
-- Name: account_security; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.account_security (
    account_id uuid NOT NULL,
    email_verified boolean DEFAULT false,
    phone_verified boolean DEFAULT false,
    two_factor_enabled boolean DEFAULT false
);


ALTER TABLE public.account_security OWNER TO postgres;

--
-- Name: accounts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.accounts (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    last_login timestamp without time zone,
    status integer DEFAULT 1
);


ALTER TABLE public.accounts OWNER TO postgres;

--
-- Name: breakthrough_attempts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.breakthrough_attempts (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    character_id uuid,
    realm_id integer,
    success_rate double precision,
    result boolean,
    created_at timestamp without time zone DEFAULT now()
);


ALTER TABLE public.breakthrough_attempts OWNER TO postgres;

--
-- Name: breakthrough_conditions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.breakthrough_conditions (
    id integer NOT NULL,
    realm_id integer NOT NULL,
    condition_type character varying(50),
    target_id integer,
    success_bonus double precision,
    max_stack integer DEFAULT 1
);


ALTER TABLE public.breakthrough_conditions OWNER TO postgres;

--
-- Name: breakthrough_conditions_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.breakthrough_conditions_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.breakthrough_conditions_id_seq OWNER TO postgres;

--
-- Name: breakthrough_conditions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.breakthrough_conditions_id_seq OWNED BY public.breakthrough_conditions.id;


--
-- Name: character_base_stats; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.character_base_stats (
    character_id uuid NOT NULL,
    realm_id integer,
    cultivation bigint DEFAULT 0,
    base_hp integer DEFAULT 100,
    base_mp integer DEFAULT 100,
    base_physique integer DEFAULT 10,
    base_attack integer DEFAULT 10,
    base_move_speed numeric(10,4) DEFAULT 100.0,
    base_speed integer DEFAULT 10,
    base_spiritual_sense integer DEFAULT 10,
    base_fortune double precision DEFAULT 0.01,
    base_potential integer DEFAULT 0,
    base_stamina integer DEFAULT 100,
    lifespan_bonus integer DEFAULT 0
);


ALTER TABLE public.character_base_stats OWNER TO postgres;

--
-- Name: character_current_state; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.character_current_state (
    character_id uuid NOT NULL,
    current_hp integer DEFAULT 100 NOT NULL,
    current_mp integer DEFAULT 100 NOT NULL,
    current_map_id integer,
    current_zone_index integer DEFAULT 0 NOT NULL,
    current_pos_x real DEFAULT 0 NOT NULL,
    current_pos_y real DEFAULT 0 NOT NULL,
    is_dead boolean DEFAULT false NOT NULL,
    current_state integer DEFAULT 0 NOT NULL,
    last_saved_at timestamp without time zone DEFAULT now() NOT NULL,
    current_stamina integer DEFAULT 100 NOT NULL,
    lifespan_end_game_minute bigint DEFAULT 0 NOT NULL
);


ALTER TABLE public.character_current_state OWNER TO postgres;

--
-- Name: characters; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.characters (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    account_id uuid NOT NULL,
    server_id integer NOT NULL,
    name character varying(50) NOT NULL,
    model_id integer DEFAULT 1,
    gender integer DEFAULT 0,
    hair_color integer DEFAULT 0,
    eye_color integer DEFAULT 0,
    face_id integer DEFAULT 0,
    created_at timestamp without time zone DEFAULT now()
);


ALTER TABLE public.characters OWNER TO postgres;

--
-- Name: game_time_state; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.game_time_state (
    id integer NOT NULL,
    anchor_utc timestamp with time zone NOT NULL,
    anchor_game_minute bigint NOT NULL,
    game_minutes_per_real_minute double precision NOT NULL,
    days_per_game_year integer NOT NULL,
    runtime_save_interval_seconds integer DEFAULT 2 NOT NULL,
    derived_state_refresh_interval_seconds integer DEFAULT 5 NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.game_time_state OWNER TO postgres;

--
-- Name: map_template_adjacent_maps; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.map_template_adjacent_maps (
    map_template_id integer NOT NULL,
    adjacent_map_template_id integer NOT NULL
);


ALTER TABLE public.map_template_adjacent_maps OWNER TO postgres;

--
-- Name: map_templates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.map_templates (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    map_type integer NOT NULL,
    client_map_key character varying(100) NOT NULL,
    width real DEFAULT 0 NOT NULL,
    height real DEFAULT 0 NOT NULL,
    cell_size real DEFAULT 1 NOT NULL,
    default_spawn_x real DEFAULT 0 NOT NULL,
    default_spawn_y real DEFAULT 0 NOT NULL,
    max_public_zone_count integer DEFAULT 0 NOT NULL,
    max_players_per_zone integer DEFAULT 1 NOT NULL,
    is_private_per_player boolean DEFAULT false NOT NULL,
    created_at timestamp without time zone DEFAULT now()
);


ALTER TABLE public.map_templates OWNER TO postgres;

--
-- Name: realm_templates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.realm_templates (
    id integer NOT NULL,
    name character varying(50),
    stage_name character varying(50),
    max_cultivation bigint,
    base_breakthrough_rate double precision,
    failure_penalty double precision DEFAULT 0,
    created_at timestamp without time zone DEFAULT now(),
    lifespan integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.realm_templates OWNER TO postgres;

--
-- Name: servers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.servers (
    id integer NOT NULL,
    name character varying(50),
    status integer DEFAULT 1
);


ALTER TABLE public.servers OWNER TO postgres;

--
-- Name: breakthrough_conditions id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions ALTER COLUMN id SET DEFAULT nextval('public.breakthrough_conditions_id_seq'::regclass);


--
-- Data for Name: account_credentials; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.account_credentials (id, account_id, provider, provider_user_id, password_hash, created_at) FROM stdin;
d0c92f2b-d4ef-4d7c-b9c5-2bbb942e7858	53cecbca-1efe-4883-8569-dfccf96db7bc	password	testuser	PBKDF2-SHA256$200000$wtF5/hNAx6anVT2mBC5OVg==$rF1eVqMn/FBpv64o0EFwy56V1yrrttrtSYcL+7eW8mo=	2026-03-12 13:22:26.046208
4d064dc1-c5bb-479b-8b93-5034cbb8d6cb	f6eba63d-f391-4529-bba5-194ff0f44772	password	khoivu	PBKDF2-SHA256$200000$bfNEzpdy30OqoEmOTRGY8Q==$M8S4jDmUd+mkZNywlsHLFm7fomA5ddCI6Fnp9uiYPzY=	2026-03-12 15:26:04.729856
87430d4a-8f97-4423-971e-617a07ad26ee	5690ff56-2f6b-47fb-a7da-ca3e7ffe1dfc	password	admin2	PBKDF2-SHA256$200000$BjR4h79ARtH/0HTt8Qeryw==$NqYuk9S+opAJSBoKaDf0EVZLhLqE4JklkX129w6Cg50=	2026-03-13 06:58:48.326518
06c756b6-79cc-4706-9237-6737995d6b3a	b58e9063-fca9-41d6-b8e3-3e1998d9018f	password	test00122	PBKDF2-SHA256$200000$yKI86D7QQFwM4dTzZO4eEQ==$6CaobAJCON13AgMi/C8pWfvhdR9Rp8r5YCcT6QQMGKA=	2026-03-13 07:55:50.210031
\.


--
-- Data for Name: account_security; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.account_security (account_id, email_verified, phone_verified, two_factor_enabled) FROM stdin;
\.


--
-- Data for Name: accounts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.accounts (id, created_at, last_login, status) FROM stdin;
53cecbca-1efe-4883-8569-dfccf96db7bc	2026-03-12 13:22:26.028073	2026-03-12 13:50:21.997786	1
f6eba63d-f391-4529-bba5-194ff0f44772	2026-03-12 15:26:04.711812	2026-03-12 16:31:24.741499	1
5690ff56-2f6b-47fb-a7da-ca3e7ffe1dfc	2026-03-13 06:58:48.308527	2026-03-13 07:00:14.585005	1
b58e9063-fca9-41d6-b8e3-3e1998d9018f	2026-03-13 07:55:50.190011	2026-03-13 08:04:36.17559	1
\.


--
-- Data for Name: breakthrough_attempts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.breakthrough_attempts (id, character_id, realm_id, success_rate, result, created_at) FROM stdin;
\.


--
-- Data for Name: breakthrough_conditions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.breakthrough_conditions (id, realm_id, condition_type, target_id, success_bonus, max_stack) FROM stdin;
\.


--
-- Data for Name: character_base_stats; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.character_base_stats (character_id, realm_id, cultivation, base_hp, base_mp, base_physique, base_attack, base_move_speed, base_speed, base_spiritual_sense, base_fortune, base_potential, base_stamina, lifespan_bonus) FROM stdin;
77b30f1d-0ef7-4687-adbe-f9cba0f6d3fa	1	0	100	100	10	10	100.0000	10	10	0.01	0	100	0
28fc9149-7910-4d95-a2a5-6c04a6d4b786	1	0	100	100	10	10	100.0000	10	10	0.01	0	100	0
836b24d1-7c65-4365-a546-1e786c2c0854	1	0	100	100	10	10	100.0000	10	10	0.01	0	100	0
\.


--
-- Data for Name: character_current_state; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.character_current_state (character_id, current_hp, current_mp, current_map_id, current_zone_index, current_pos_x, current_pos_y, is_dead, current_state, last_saved_at, current_stamina, lifespan_end_game_minute) FROM stdin;
77b30f1d-0ef7-4687-adbe-f9cba0f6d3fa	100	100	\N	0	0	0	f	0	2026-03-13 12:26:38.622264	100	210024951
28fc9149-7910-4d95-a2a5-6c04a6d4b786	100	100	\N	0	0	0	f	0	2026-03-13 06:58:48.718836	100	210036689
836b24d1-7c65-4365-a546-1e786c2c0854	100	100	\N	0	0	0	f	0	2026-03-13 07:55:50.573642	100	106986343853
\.


--
-- Data for Name: characters; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.characters (id, account_id, server_id, name, model_id, gender, hair_color, eye_color, face_id, created_at) FROM stdin;
77b30f1d-0ef7-4687-adbe-f9cba0f6d3fa	f6eba63d-f391-4529-bba5-194ff0f44772	1	khoivu_1103	1	\N	\N	\N	\N	2026-03-12 16:31:24.915696
28fc9149-7910-4d95-a2a5-6c04a6d4b786	5690ff56-2f6b-47fb-a7da-ca3e7ffe1dfc	1	Lệ Phi Vũ	1	1	1	1	1	2026-03-13 06:58:48.708741
836b24d1-7c65-4365-a546-1e786c2c0854	b58e9063-fca9-41d6-b8e3-3e1998d9018f	1	HanLi	1	1	1	1	1	2026-03-13 07:55:50.562788
\.


--
-- Data for Name: game_time_state; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.game_time_state (id, anchor_utc, anchor_game_minute, game_minutes_per_real_minute, days_per_game_year, runtime_save_interval_seconds, derived_state_refresh_interval_seconds, updated_at) FROM stdin;
1	2026-03-13 14:42:08.124049+07	106917029983	518400	360	2	5	2026-03-13 14:42:08.124049+07
\.


--
--
-- Data for Name: map_template_adjacent_maps; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.map_template_adjacent_maps (map_template_id, adjacent_map_template_id) FROM stdin;
1	2
2	1
\.


--
-- Data for Name: map_templates; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.map_templates (id, name, map_type, client_map_key, width, height, cell_size, default_spawn_x, default_spawn_y, max_public_zone_count, max_players_per_zone, is_private_per_player, created_at) FROM stdin;
1	Player Home	0	map_home_01	256	256	32	64	64	0	1	t	2026-03-14 16:00:00
2	Starter Plains	1	map_farm_01	1024	1024	64	128	128	2	20	f	2026-03-14 16:00:00
\.


--
-- Data for Name: realm_templates; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.realm_templates (id, name, stage_name, max_cultivation, base_breakthrough_rate, failure_penalty, created_at, lifespan) FROM stdin;
1	Luyện Khí Kỳ tầng 1	Luyện Khí Kỳ tầng 1	150	100	0	2026-03-12 23:28:24.439415	120
2	Luyện Khí Kỳ tầng 2	Luyện Khí Kỳ tầng 2	200	95	0	2026-03-12 23:28:24.439415	125
3	Luyện Khí Kỳ tầng 3	Luyện Khí Kỳ tầng 3	280	90	0	2026-03-13 13:14:08.306293	130
4	Luyện Khí Kỳ tầng 4	Luyện Khí Kỳ tầng 4	380	85	0	2026-03-13 13:14:08.306293	135
5	Luyện Khí Kỳ tầng 5	Luyện Khí Kỳ tầng 5	520	80	0	2026-03-13 13:14:08.306293	140
6	Luyện Khí Kỳ tầng 6	Luyện Khí Kỳ tầng 6	750	75	0	2026-03-13 13:14:08.306293	145
7	Luyện Khí Kỳ tầng 7	Luyện Khí Kỳ tầng 7	1200	70	0	2026-03-13 13:14:08.306293	150
8	Luyện Khí Kỳ tầng 8	Luyện Khí Kỳ tầng 8	1500	65	0	2026-03-13 13:14:08.306293	155
9	Luyện Khí Kỳ tầng 9	Luyện Khí Kỳ tầng 9	2000	40	0	2026-03-13 13:14:08.306293	160
10	Trúc Cơ Sơ Kỳ	Trúc Cơ Sơ Kỳ	5000	40	0	2026-03-13 13:14:08.306293	180
11	Trúc Cơ Trung Kỳ	Trúc Cơ Trung Kỳ	7000	35	0	2026-03-13 13:14:08.306293	200
12	Trúc Cơ Hậu Kỳ	Trúc Cơ Hậu Kỳ	10000	25	0	2026-03-13 13:14:08.306293	220
13	Kết Đan Sơ Kỳ	Kết Đan Sơ Kỳ	25000	30	0	2026-03-13 13:14:08.306293	350
14	Kết Đan Trung Kỳ	Kết Đan Trung Kỳ	35000	28	0	2026-03-13 13:14:08.306293	400
15	Kết Đan Hậu Kỳ	Kết Đan Hậu Kỳ	50000	20	0	2026-03-13 13:14:08.306293	500
16	Nguyên Anh Sơ Kỳ	Nguyên Anh Sơ Kỳ	125000	18	0	2026-03-13 13:14:08.306293	1200
17	Nguyên Anh Trung Kỳ	Nguyên Anh Trung Kỳ	175000	15	0	2026-03-13 13:14:08.306293	1500
18	Nguyên Anh Hậu Kỳ	Nguyên Anh Hậu Kỳ	245000	10	0	2026-03-13 13:14:08.306293	2000
19	Hóa Thần Sơ Kỳ	Hóa Thần Sơ Kỳ	600000	60	0	2026-03-13 13:14:08.306293	-1
20	Hóa Thần Trung Kỳ	Hóa Thần Trung Kỳ	840000	55	0	2026-03-13 13:14:08.306293	-1
21	Hóa Thần Hậu Kỳ	Hóa Thần Hậu Kỳ	1200000	30	0	2026-03-13 13:14:08.306293	-1
22	Luyện Hư Sơ Kỳ	Luyện Hư Sơ Kỳ	3000000	30	0	2026-03-13 13:14:08.306293	-1
23	Luyện Hư Trung Kỳ	Luyện Hư Trung Kỳ	4200000	25	0	2026-03-13 13:14:08.306293	-1
24	Luyện Hư Hậu Kỳ	Luyện Hư Hậu Kỳ	9000000	15	0	2026-03-13 13:14:08.306293	-1
25	Hợp Thể Sơ Kỳ	Hợp Thể Sơ Kỳ	20000000	20	0	2026-03-13 13:14:08.306293	-1
26	Hợp Thể Trung Kỳ	Hợp Thể Trung Kỳ	28000000	15	0	2026-03-13 13:14:08.306293	-1
27	Hợp Thể Hậu Kỳ	Hợp Thể Hậu Kỳ	40000000	10	0	2026-03-13 13:14:08.306293	-1
28	Độ Kiếp Kỳ	Độ Kiếp Kỳ	100000000	12	0	2026-03-13 13:14:08.306293	-1
29	Chân Tiên Sơ Kỳ	Chân Tiên Sơ Kỳ	250000000	6	0	2026-03-13 13:14:08.306293	-1
30	Chân Tiên Trung Kỳ	Chân Tiên Trung Kỳ	350000000	5	0	2026-03-13 13:14:08.306293	-1
31	Chân Tiên Hậu Kỳ	Chân Tiên Hậu Kỳ	500000000	4	0	2026-03-13 13:14:08.306293	-1
\.


--
-- Data for Name: servers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.servers (id, name, status) FROM stdin;
1	Server01	1
\.


--
-- Name: breakthrough_conditions_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.breakthrough_conditions_id_seq', 1, false);


--
-- Name: account_credentials account_credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT account_credentials_pkey PRIMARY KEY (id);


--
-- Name: account_security account_security_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_security
    ADD CONSTRAINT account_security_pkey PRIMARY KEY (account_id);


--
-- Name: accounts accounts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts
    ADD CONSTRAINT accounts_pkey PRIMARY KEY (id);


--
-- Name: breakthrough_attempts breakthrough_attempts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_attempts
    ADD CONSTRAINT breakthrough_attempts_pkey PRIMARY KEY (id);


--
-- Name: breakthrough_conditions breakthrough_conditions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions
    ADD CONSTRAINT breakthrough_conditions_pkey PRIMARY KEY (id);


--
-- Name: character_base_stats character_base_stats_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_base_stats
    ADD CONSTRAINT character_base_stats_pkey PRIMARY KEY (character_id);


--
-- Name: character_current_state character_current_state_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_current_state
    ADD CONSTRAINT character_current_state_pkey PRIMARY KEY (character_id);


--
-- Name: map_template_adjacent_maps map_template_adjacent_maps_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.map_template_adjacent_maps
    ADD CONSTRAINT map_template_adjacent_maps_pkey PRIMARY KEY (map_template_id, adjacent_map_template_id);


--
-- Name: map_templates map_templates_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.map_templates
    ADD CONSTRAINT map_templates_pkey PRIMARY KEY (id);


--
-- Name: characters characters_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT characters_name_key UNIQUE (name);


--
-- Name: characters characters_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT characters_pkey PRIMARY KEY (id);


--
-- Name: game_time_state game_time_state_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.game_time_state
    ADD CONSTRAINT game_time_state_pkey PRIMARY KEY (id);


--
-- Name: realm_templates realm_templates_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.realm_templates
    ADD CONSTRAINT realm_templates_pkey PRIMARY KEY (id);


--
-- Name: servers servers_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.servers
    ADD CONSTRAINT servers_pkey PRIMARY KEY (id);


--
-- Name: account_credentials unique_provider_user; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT unique_provider_user UNIQUE (provider, provider_user_id);


--
-- Name: idx_character_account; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_character_account ON public.characters USING btree (account_id);


--
-- Name: idx_character_current_state_map_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_character_current_state_map_id ON public.character_current_state USING btree (current_map_id);


--
-- Name: idx_character_current_state_map_zone; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_character_current_state_map_zone ON public.character_current_state USING btree (current_map_id, current_zone_index);


--
-- Name: breakthrough_attempts fk_attempt_character; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_attempts
    ADD CONSTRAINT fk_attempt_character FOREIGN KEY (character_id) REFERENCES public.characters(id);


--
-- Name: characters fk_character_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT fk_character_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- Name: character_base_stats fk_character_base_stats_character; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_base_stats
    ADD CONSTRAINT fk_character_base_stats_character FOREIGN KEY (character_id) REFERENCES public.characters(id) ON DELETE CASCADE;


--
-- Name: character_current_state fk_character_current_state_character; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_current_state
    ADD CONSTRAINT fk_character_current_state_character FOREIGN KEY (character_id) REFERENCES public.characters(id) ON DELETE CASCADE;


--
-- Name: map_template_adjacent_maps fk_map_template_adjacent_maps_source; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.map_template_adjacent_maps
    ADD CONSTRAINT fk_map_template_adjacent_maps_source FOREIGN KEY (map_template_id) REFERENCES public.map_templates(id) ON DELETE CASCADE;


--
-- Name: map_template_adjacent_maps fk_map_template_adjacent_maps_target; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.map_template_adjacent_maps
    ADD CONSTRAINT fk_map_template_adjacent_maps_target FOREIGN KEY (adjacent_map_template_id) REFERENCES public.map_templates(id) ON DELETE CASCADE;


--
-- Name: characters fk_character_server; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT fk_character_server FOREIGN KEY (server_id) REFERENCES public.servers(id);


--
-- Name: breakthrough_conditions fk_condition_realm; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions
    ADD CONSTRAINT fk_condition_realm FOREIGN KEY (realm_id) REFERENCES public.realm_templates(id);


--
-- Name: account_credentials fk_credential_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT fk_credential_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- Name: account_security fk_security_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_security
    ADD CONSTRAINT fk_security_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict lfv5QTJmP6RrK1GA6jzzEg7DuzvjutlNL58PWRb83Vb3T8OciBqxtEbs4X2n5iL

