-- حذف الحساب من داخل اللعبة (زر «حذف الحساب» في الإعدادات)
-- انسخي كل هذا والصقيه في Supabase: SQL Editor ← New query ← Run
-- آمن لو انشغّل أكثر من مرة

-- ١. اللاعب يقدر يحذف صفه هو بس من جدول اللاعبين
drop policy if exists "naaqa_delete_own" on public.players;
create policy "naaqa_delete_own" on public.players
  for delete using (auth.uid() = id);

-- ٢. دالة تحذف حساب اللاعب اللي داخل (هو بس، ما تقدر تحذف أحد ثاني)
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
