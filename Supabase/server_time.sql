-- وقت السيرفر للعبة (المكافأة اليومية، الفزعة، وقت الغياب)
-- اللعبة على المتصفح ما تقدر تقرا الوقت من رأس الرد، فتسأل هالدالة
-- انسخي كل هذا والصقيه في Supabase: SQL Editor ← New query ← Run
-- آمن لو انشغّل أكثر من مرة، وما يلمس أي بيانات

create or replace function public.server_now()
returns timestamptz
language sql
stable
as $$ select now() $$;

grant execute on function public.server_now() to anon, authenticated;
