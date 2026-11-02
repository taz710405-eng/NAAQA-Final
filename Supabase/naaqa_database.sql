-- =====================================================================
--  ناقة (NAAQA) – قاعدة البيانات كاملة (Supabase / PostgreSQL)
--  الملف هذا يجمع كل اللي تحتاجه اللعبة في قاعدة البيانات في مكان واحد.
--  التشغيل: Supabase ← SQL Editor ← New query ← الصق الملف كامل ← Run
--  آمن لو انشغّل أكثر من مرة، وما يمسح بيانات اللاعبين الموجودة.
--
--  نظرة عامة:
--   • تسجيل الدخول: Supabase Auth (البريد + رمز تحقق). جدول auth.users يسويه Supabase لحاله.
--   • جدول public.players: صف واحد لكل لاعب، فيه اسمه وحفظ اللعبة كامل (save_data بصيغة JSON).
--   • الحماية (RLS): كل لاعب يقرأ ويكتب صفه هو بس.
--   • دوال: delete_my_account (حذف الحساب من داخل اللعبة)، server_now (وقت السيرفر).
--   • المزامنة: اللعبة ترفع الحفظ برفع مشروط على رقم النسخة save_data->>'rev'
--     (تحديث واحد UPDATE ... WHERE)، فلو جهازين رفعوا بنفس اللحظة واحد بس ينجح.
-- =====================================================================

-- ١. جدول اللاعبين
create table if not exists public.players (
  id           uuid primary key references auth.users (id) on delete cascade,
  display_name text,
  save_data    jsonb,                    -- حفظ اللعبة كامل (SaveManager.SaveData)
  updated_at   timestamptz default now()
);

-- لو الجدول موجود من قبل بدون أعمدة الحفظ
alter table public.players
  add column if not exists display_name text,
  add column if not exists save_data jsonb,
  add column if not exists updated_at timestamptz default now();

-- ٢. الحماية: كل لاعب يقرأ ويكتب ويحذف صفه هو بس
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

drop policy if exists "naaqa_delete_own" on public.players;
create policy "naaqa_delete_own" on public.players
  for delete using (auth.uid() = id);

-- ٣. حذف الحساب (زر «حذف الحساب» في الإعدادات): يحذف صف اللاعب وحسابه، له هو بس
create or replace function public.delete_my_account()
returns void
language plpgsql
security definer
set search_path = public, auth
as $$
declare
  uid uuid := auth.uid();
begin
  if uid is null then
    raise exception 'not logged in';
  end if;
  delete from public.players where id = uid;
  delete from auth.users where id = uid;
end;
$$;

revoke all on function public.delete_my_account() from public, anon;
grant execute on function public.delete_my_account() to authenticated;

-- ٤. وقت السيرفر (المكافأة اليومية، المساعدة الطارئة، وقت الغياب)
--    اللعبة على المتصفح ما تقدر تقرا وقت السيرفر من رأس الرد، فتسأل هالدالة
create or replace function public.server_now()
returns timestamptz
language sql
stable
as $$ select now() $$;

grant execute on function public.server_now() to anon, authenticated;

-- =====================================================================
--  أوامر مفيدة للمراجعة (ما تغيّر شي):
--   select id, display_name, updated_at, save_data->>'rev' as rev from public.players;
--   select policyname, cmd from pg_policies where tablename = 'players';
-- =====================================================================
