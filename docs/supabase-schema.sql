create table if not exists public.player_profiles (
    player_id uuid primary key,
    email text not null unique,
    display_name text not null,
    country_code text not null default 'US',
    favorite_category text not null default 'Memory',
    joined_at_utc timestamptz not null default timezone('utc', now())
);

create table if not exists public.games (
    game_id text primary key,
    name text not null,
    category text not null,
    description text not null,
    difficulty text not null
);

create table if not exists public.lessons (
    lesson_id text primary key,
    game_id text not null references public.games(game_id),
    locale text not null,
    title text not null,
    summary text not null,
    takeaway text not null
);

create table if not exists public.score_attempts (
    attempt_id uuid primary key,
    player_id uuid not null references public.player_profiles(player_id),
    game_id text not null references public.games(game_id),
    score integer not null,
    moves integer not null,
    duration_seconds integer not null,
    session_seed integer not null,
    matched_pairs integer not null,
    max_pairs integer not null,
    completed_at_utc timestamptz not null
);
