--
-- PostgreSQL database dump
--

\restrict 43tp9h5QWazg0xgGLOJe7zSb57wtGGA5onibRb0J1gS4OvQv0l2gndYuZk7wpcH

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

-- Started on 2026-03-10 23:41:12

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
-- TOC entry 2 (class 3079 OID 16389)
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- TOC entry 5021 (class 0 OID 0)
-- Dependencies: 2
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 221 (class 1259 OID 16409)
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
-- TOC entry 222 (class 1259 OID 16429)
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
-- TOC entry 220 (class 1259 OID 16400)
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
-- TOC entry 229 (class 1259 OID 16522)
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
-- TOC entry 228 (class 1259 OID 16508)
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
-- TOC entry 227 (class 1259 OID 16507)
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
-- TOC entry 5022 (class 0 OID 0)
-- Dependencies: 227
-- Name: breakthrough_conditions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.breakthrough_conditions_id_seq OWNED BY public.breakthrough_conditions.id;


--
-- TOC entry 225 (class 1259 OID 16479)
-- Name: character_stats; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.character_stats (
    character_id uuid NOT NULL,
    realm_id integer,
    cultivation bigint DEFAULT 0,
    hp integer DEFAULT 100,
    mp integer DEFAULT 100,
    physique integer DEFAULT 10,
    attack integer DEFAULT 10,
    speed integer DEFAULT 10,
    spiritual_sense integer DEFAULT 10,
    fortune double precision DEFAULT 0.01,
    potential integer DEFAULT 0
);


ALTER TABLE public.character_stats OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 16450)
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
-- TOC entry 226 (class 1259 OID 16499)
-- Name: realm_templates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.realm_templates (
    id integer NOT NULL,
    name character varying(50),
    stage_name character varying(50),
    max_cultivation bigint,
    base_breakthrough_rate double precision,
    failure_penalty double precision DEFAULT 0,
    created_at timestamp without time zone DEFAULT now()
);


ALTER TABLE public.realm_templates OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16443)
-- Name: servers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.servers (
    id integer NOT NULL,
    name character varying(50),
    status integer DEFAULT 1
);


ALTER TABLE public.servers OWNER TO postgres;

--
-- TOC entry 4825 (class 2604 OID 16511)
-- Name: breakthrough_conditions id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions ALTER COLUMN id SET DEFAULT nextval('public.breakthrough_conditions_id_seq'::regclass);


--
-- TOC entry 5007 (class 0 OID 16409)
-- Dependencies: 221
-- Data for Name: account_credentials; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.account_credentials (id, account_id, provider, provider_user_id, password_hash, created_at) FROM stdin;
\.


--
-- TOC entry 5008 (class 0 OID 16429)
-- Dependencies: 222
-- Data for Name: account_security; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.account_security (account_id, email_verified, phone_verified, two_factor_enabled) FROM stdin;
\.


--
-- TOC entry 5006 (class 0 OID 16400)
-- Dependencies: 220
-- Data for Name: accounts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.accounts (id, created_at, last_login, status) FROM stdin;
\.


--
-- TOC entry 5015 (class 0 OID 16522)
-- Dependencies: 229
-- Data for Name: breakthrough_attempts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.breakthrough_attempts (id, character_id, realm_id, success_rate, result, created_at) FROM stdin;
\.


--
-- TOC entry 5014 (class 0 OID 16508)
-- Dependencies: 228
-- Data for Name: breakthrough_conditions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.breakthrough_conditions (id, realm_id, condition_type, target_id, success_bonus, max_stack) FROM stdin;
\.


--
-- TOC entry 5011 (class 0 OID 16479)
-- Dependencies: 225
-- Data for Name: character_stats; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.character_stats (character_id, realm_id, cultivation, hp, mp, physique, attack, speed, spiritual_sense, fortune, potential) FROM stdin;
\.


--
-- TOC entry 5010 (class 0 OID 16450)
-- Dependencies: 224
-- Data for Name: characters; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.characters (id, account_id, server_id, name, model_id, gender, hair_color, eye_color, face_id, created_at) FROM stdin;
\.


--
-- TOC entry 5012 (class 0 OID 16499)
-- Dependencies: 226
-- Data for Name: realm_templates; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.realm_templates (id, name, stage_name, max_cultivation, base_breakthrough_rate, failure_penalty, created_at) FROM stdin;
\.


--
-- TOC entry 5009 (class 0 OID 16443)
-- Dependencies: 223
-- Data for Name: servers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.servers (id, name, status) FROM stdin;
\.


--
-- TOC entry 5023 (class 0 OID 0)
-- Dependencies: 227
-- Name: breakthrough_conditions_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.breakthrough_conditions_id_seq', 1, false);


--
-- TOC entry 4832 (class 2606 OID 16421)
-- Name: account_credentials account_credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT account_credentials_pkey PRIMARY KEY (id);


--
-- TOC entry 4836 (class 2606 OID 16437)
-- Name: account_security account_security_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_security
    ADD CONSTRAINT account_security_pkey PRIMARY KEY (account_id);


--
-- TOC entry 4830 (class 2606 OID 16408)
-- Name: accounts accounts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts
    ADD CONSTRAINT accounts_pkey PRIMARY KEY (id);


--
-- TOC entry 4851 (class 2606 OID 16529)
-- Name: breakthrough_attempts breakthrough_attempts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_attempts
    ADD CONSTRAINT breakthrough_attempts_pkey PRIMARY KEY (id);


--
-- TOC entry 4849 (class 2606 OID 16516)
-- Name: breakthrough_conditions breakthrough_conditions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions
    ADD CONSTRAINT breakthrough_conditions_pkey PRIMARY KEY (id);


--
-- TOC entry 4845 (class 2606 OID 16493)
-- Name: character_stats character_stats_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_stats
    ADD CONSTRAINT character_stats_pkey PRIMARY KEY (character_id);


--
-- TOC entry 4840 (class 2606 OID 16467)
-- Name: characters characters_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT characters_name_key UNIQUE (name);


--
-- TOC entry 4842 (class 2606 OID 16465)
-- Name: characters characters_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT characters_pkey PRIMARY KEY (id);


--
-- TOC entry 4847 (class 2606 OID 16506)
-- Name: realm_templates realm_templates_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.realm_templates
    ADD CONSTRAINT realm_templates_pkey PRIMARY KEY (id);


--
-- TOC entry 4838 (class 2606 OID 16449)
-- Name: servers servers_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.servers
    ADD CONSTRAINT servers_pkey PRIMARY KEY (id);


--
-- TOC entry 4834 (class 2606 OID 16423)
-- Name: account_credentials unique_provider_user; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT unique_provider_user UNIQUE (provider, provider_user_id);


--
-- TOC entry 4843 (class 1259 OID 16478)
-- Name: idx_character_account; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_character_account ON public.characters USING btree (account_id);


--
-- TOC entry 4858 (class 2606 OID 16530)
-- Name: breakthrough_attempts fk_attempt_character; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_attempts
    ADD CONSTRAINT fk_attempt_character FOREIGN KEY (character_id) REFERENCES public.characters(id);


--
-- TOC entry 4854 (class 2606 OID 16468)
-- Name: characters fk_character_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT fk_character_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- TOC entry 4855 (class 2606 OID 16473)
-- Name: characters fk_character_server; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.characters
    ADD CONSTRAINT fk_character_server FOREIGN KEY (server_id) REFERENCES public.servers(id);


--
-- TOC entry 4857 (class 2606 OID 16517)
-- Name: breakthrough_conditions fk_condition_realm; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.breakthrough_conditions
    ADD CONSTRAINT fk_condition_realm FOREIGN KEY (realm_id) REFERENCES public.realm_templates(id);


--
-- TOC entry 4852 (class 2606 OID 16424)
-- Name: account_credentials fk_credential_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_credentials
    ADD CONSTRAINT fk_credential_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- TOC entry 4853 (class 2606 OID 16438)
-- Name: account_security fk_security_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.account_security
    ADD CONSTRAINT fk_security_account FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE;


--
-- TOC entry 4856 (class 2606 OID 16494)
-- Name: character_stats fk_stats_character; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.character_stats
    ADD CONSTRAINT fk_stats_character FOREIGN KEY (character_id) REFERENCES public.characters(id) ON DELETE CASCADE;


-- Completed on 2026-03-10 23:41:13

--
-- PostgreSQL database dump complete
--

\unrestrict 43tp9h5QWazg0xgGLOJe7zSb57wtGGA5onibRb0J1gS4OvQv0l2gndYuZk7wpcH

