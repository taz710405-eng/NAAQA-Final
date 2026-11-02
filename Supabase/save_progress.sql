-- حفظ تقدم اللاعب في ناقة
-- انسخي كل هذا والصقيه في Supabase: SQL Editor ← New query ← Run
-- آمن لو انشغّل أكثر من مرة

-- ١. أعمدة الحفظ في جدول اللاعبين
alter table public.players
  add column if not exists save_data jsonb,
  add column if not exists updated_at timestamptz default now();

-- ٢. الحماية: كل لاعب يقرأ ويكتب صفه هو بس
alter table public.players enable row level security;

drop policy if exists "naaqa_select_own" on public.players;
create policy "naaqa_select_own" on public.players
  for select using (auth.uid() = id);

drop policy if exists "naaqa_insert_own" on public.players;
create policy "naaqa_insert_own" on public.players
  for insert with check (auth.uid() = id);

drop policy if exists "naaqa_update_own" on public.players;
create policy "naaqa_update_own" on public.players
  for update using (auth.uid() = id) with check (auth.uid() = id);
