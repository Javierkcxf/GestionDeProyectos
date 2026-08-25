-- ============================================================
-- Procedimientos/funciones almacenados para Postgres (Supabase)
-- Puerto de los stored procedures de SQL Server (BaseProyecto) usados
-- por FrontBlazor. Pegar completo en el SQL Editor de Supabase y ejecutar.
--
-- Nota importante: las rutinas que el frontend necesita LEER resultados
-- (listados, "NuevoId", mensajes) están creadas como FUNCTION que
-- devuelve TABLE(...), porque el backend solo captura filas de
-- resultado cuando la rutina es FUNCTION (a PROCEDURE se ejecuta con
-- CALL y su resultado se descarta). Las rutinas de solo mutación que el
-- frontend nunca lee (fire-and-forget + recarga de lista) quedan como
-- PROCEDURE real.
-- ============================================================

-- ================= ROL =================

CREATE OR REPLACE FUNCTION sp_seleccionarrol(p_id integer DEFAULT NULL)
RETURNS TABLE(id integer, nombre varchar) AS $$
BEGIN
  RETURN QUERY SELECT r.id, r.nombre FROM rol r
  WHERE (p_id IS NULL OR r.id = p_id)
  ORDER BY r.nombre;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_insertarrol(Nombre varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF EXISTS (SELECT 1 FROM rol r WHERE r.nombre = sp_insertarrol.Nombre) THEN
    RAISE EXCEPTION 'El nombre del rol ya existe.';
  END IF;
  INSERT INTO rol (nombre) VALUES (sp_insertarrol.Nombre);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarrol(Id integer, Nombre varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM rol r WHERE r.id = sp_actualizarrol.Id) THEN
    RAISE EXCEPTION 'El rol no existe.';
  END IF;
  IF EXISTS (SELECT 1 FROM rol r WHERE r.nombre = sp_actualizarrol.Nombre AND r.id <> sp_actualizarrol.Id) THEN
    RAISE EXCEPTION 'Ya existe otro rol con ese nombre.';
  END IF;
  UPDATE rol SET nombre = sp_actualizarrol.Nombre WHERE id = sp_actualizarrol.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarrol(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM rol r WHERE r.id = sp_eliminarrol.Id) THEN
    RAISE EXCEPTION 'El rol no existe.';
  END IF;
  DELETE FROM rol WHERE id = sp_eliminarrol.Id;
END;
$$;

-- ================= RUTA =================

CREATE OR REPLACE FUNCTION sp_seleccionarruta(p_ruta varchar DEFAULT NULL)
RETURNS TABLE(ruta varchar, descripcion varchar) AS $$
BEGIN
  RETURN QUERY SELECT rt.ruta, rt.descripcion FROM ruta rt
  WHERE (p_ruta IS NULL OR rt.ruta = p_ruta)
  ORDER BY rt.ruta;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_insertarruta(Ruta varchar, Descripcion varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF EXISTS (SELECT 1 FROM ruta rt WHERE rt.ruta = sp_insertarruta.Ruta) THEN
    RAISE EXCEPTION 'La ruta especificada ya existe.';
  END IF;
  INSERT INTO ruta (ruta, descripcion) VALUES (sp_insertarruta.Ruta, sp_insertarruta.Descripcion);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarruta(RutaOriginal varchar, RutaNueva varchar, Descripcion varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM ruta rt WHERE rt.ruta = sp_actualizarruta.RutaOriginal) THEN
    RAISE EXCEPTION 'La ruta original no existe.';
  END IF;
  IF sp_actualizarruta.RutaOriginal <> sp_actualizarruta.RutaNueva
     AND EXISTS (SELECT 1 FROM ruta rt WHERE rt.ruta = sp_actualizarruta.RutaNueva) THEN
    RAISE EXCEPTION 'La nueva ruta ya existe.';
  END IF;
  UPDATE ruta SET ruta = sp_actualizarruta.RutaNueva, descripcion = sp_actualizarruta.Descripcion
  WHERE ruta = sp_actualizarruta.RutaOriginal;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarruta(Ruta varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM ruta rt WHERE rt.ruta = sp_eliminarruta.Ruta) THEN
    RAISE EXCEPTION 'La ruta no existe.';
  END IF;
  DELETE FROM ruta WHERE ruta = sp_eliminarruta.Ruta;
END;
$$;

-- ================= RUTAROL (PERMISOS) =================

CREATE OR REPLACE FUNCTION listar_rutarol()
RETURNS TABLE(ruta varchar, rol varchar) AS $$
BEGIN
  RETURN QUERY SELECT rr.ruta, rr.rol FROM rutarol rr ORDER BY rr.ruta, rr.rol;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE crear_rutarol(p_ruta varchar, p_rol varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM ruta rt WHERE rt.ruta = crear_rutarol.p_ruta) THEN
    RAISE EXCEPTION 'La ruta especificada no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM rol r WHERE r.nombre = crear_rutarol.p_rol) THEN
    RAISE EXCEPTION 'El rol especificado no existe.';
  END IF;
  IF EXISTS (SELECT 1 FROM rutarol rr WHERE rr.ruta = crear_rutarol.p_ruta AND rr.rol = crear_rutarol.p_rol) THEN
    RAISE EXCEPTION 'El permiso ya existe. Este rol ya tiene acceso a esta ruta.';
  END IF;
  INSERT INTO rutarol (ruta, rol) VALUES (crear_rutarol.p_ruta, crear_rutarol.p_rol);
END;
$$;

CREATE OR REPLACE PROCEDURE eliminar_rutarol(p_ruta varchar, p_rol varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM rutarol rr WHERE rr.ruta = eliminar_rutarol.p_ruta AND rr.rol = eliminar_rutarol.p_rol) THEN
    RAISE EXCEPTION 'El permiso no existe.';
  END IF;
  DELETE FROM rutarol WHERE ruta = eliminar_rutarol.p_ruta AND rol = eliminar_rutarol.p_rol;
END;
$$;

-- ================= USUARIOS CON ROLES =================

CREATE OR REPLACE FUNCTION listar_usuarios_con_roles()
RETURNS TABLE(resultado json) AS $$
BEGIN
  RETURN QUERY
  SELECT json_agg(
    json_build_object(
      'Id', u."Id",
      'Email', u."Email",
      'RutaAvatar', u."RutaAvatar",
      'Activo', u."Activo",
      'Roles', COALESCE((
        SELECT json_agg(json_build_object('IdRol', r.id, 'Nombre', r.nombre))
        FROM rol_usuario ru
        INNER JOIN rol r ON ru.fkidrol = r.id
        WHERE ru.fkemail = u."Email"
      ), '[]'::json)
    )
  )
  FROM usuario u
  WHERE u."Activo" = true;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE crear_usuario_con_roles(p_email varchar, p_contrasena varchar, p_roles jsonb)
LANGUAGE plpgsql AS $$
BEGIN
  IF EXISTS (SELECT 1 FROM usuario u WHERE u."Email" = crear_usuario_con_roles.p_email) THEN
    RAISE EXCEPTION 'El email ya está registrado.';
  END IF;
  INSERT INTO usuario ("Email", "Contrasena", "Activo")
  VALUES (crear_usuario_con_roles.p_email, crear_usuario_con_roles.p_contrasena, true);
  INSERT INTO rol_usuario (fkemail, fkidrol)
  SELECT crear_usuario_con_roles.p_email, (elem->>'fkidrol')::integer
  FROM jsonb_array_elements(crear_usuario_con_roles.p_roles) elem;
END;
$$;

CREATE OR REPLACE PROCEDURE actualizar_usuario_con_roles(p_email varchar, p_contrasena varchar, p_roles jsonb)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM usuario u WHERE u."Email" = actualizar_usuario_con_roles.p_email) THEN
    RAISE EXCEPTION 'El usuario no existe.';
  END IF;
  IF actualizar_usuario_con_roles.p_contrasena IS NOT NULL AND length(actualizar_usuario_con_roles.p_contrasena) > 0 THEN
    UPDATE usuario SET "Contrasena" = actualizar_usuario_con_roles.p_contrasena
    WHERE "Email" = actualizar_usuario_con_roles.p_email;
  END IF;
  DELETE FROM rol_usuario WHERE fkemail = actualizar_usuario_con_roles.p_email;
  INSERT INTO rol_usuario (fkemail, fkidrol)
  SELECT actualizar_usuario_con_roles.p_email, (elem->>'fkidrol')::integer
  FROM jsonb_array_elements(actualizar_usuario_con_roles.p_roles) elem;
END;
$$;

CREATE OR REPLACE PROCEDURE eliminar_usuario_con_roles(p_email varchar)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM usuario u WHERE u."Email" = eliminar_usuario_con_roles.p_email) THEN
    RAISE EXCEPTION 'El usuario no existe.';
  END IF;
  DELETE FROM rol_usuario WHERE fkemail = eliminar_usuario_con_roles.p_email;
  DELETE FROM usuario WHERE "Email" = eliminar_usuario_con_roles.p_email;
END;
$$;

-- ================= ACTIVIDAD =================

CREATE OR REPLACE PROCEDURE sp_insertaractividad(
  IdEntregable integer, Titulo varchar, Descripcion text DEFAULT NULL,
  FechaInicio date DEFAULT NULL, FechaFinPrevista date DEFAULT NULL,
  FechaModificacion date DEFAULT NULL, FechaFinalizacion date DEFAULT NULL,
  Prioridad integer DEFAULT NULL, PorcentajeAvance integer DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM entregable e WHERE e."Id" = sp_insertaractividad.IdEntregable) THEN
    RAISE EXCEPTION 'El entregable no existe.';
  END IF;
  IF sp_insertaractividad.PorcentajeAvance IS NOT NULL AND (sp_insertaractividad.PorcentajeAvance < 0 OR sp_insertaractividad.PorcentajeAvance > 100) THEN
    RAISE EXCEPTION 'El porcentaje de avance debe estar entre 0 y 100.';
  END IF;
  INSERT INTO actividad ("IdEntregable","Titulo","Descripcion","FechaInicio","FechaFinPrevista","FechaModificacion","FechaFinalizacion","Prioridad","PorcentajeAvance")
  VALUES (sp_insertaractividad.IdEntregable, sp_insertaractividad.Titulo, sp_insertaractividad.Descripcion, sp_insertaractividad.FechaInicio, sp_insertaractividad.FechaFinPrevista, sp_insertaractividad.FechaModificacion, sp_insertaractividad.FechaFinalizacion, sp_insertaractividad.Prioridad, sp_insertaractividad.PorcentajeAvance);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizaractividad(
  Id integer, IdEntregable integer, Titulo varchar, Descripcion text DEFAULT NULL,
  FechaInicio date DEFAULT NULL, FechaFinPrevista date DEFAULT NULL,
  FechaModificacion date DEFAULT NULL, FechaFinalizacion date DEFAULT NULL,
  Prioridad integer DEFAULT NULL, PorcentajeAvance integer DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM actividad a WHERE a."Id" = sp_actualizaractividad.Id) THEN
    RAISE EXCEPTION 'La actividad no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM entregable e WHERE e."Id" = sp_actualizaractividad.IdEntregable) THEN
    RAISE EXCEPTION 'El entregable no existe.';
  END IF;
  IF sp_actualizaractividad.PorcentajeAvance IS NOT NULL AND (sp_actualizaractividad.PorcentajeAvance < 0 OR sp_actualizaractividad.PorcentajeAvance > 100) THEN
    RAISE EXCEPTION 'El porcentaje de avance debe estar entre 0 y 100.';
  END IF;
  UPDATE actividad SET
    "IdEntregable" = sp_actualizaractividad.IdEntregable,
    "Titulo" = sp_actualizaractividad.Titulo,
    "Descripcion" = sp_actualizaractividad.Descripcion,
    "FechaInicio" = sp_actualizaractividad.FechaInicio,
    "FechaFinPrevista" = sp_actualizaractividad.FechaFinPrevista,
    "FechaModificacion" = sp_actualizaractividad.FechaModificacion,
    "FechaFinalizacion" = sp_actualizaractividad.FechaFinalizacion,
    "Prioridad" = sp_actualizaractividad.Prioridad,
    "PorcentajeAvance" = sp_actualizaractividad.PorcentajeAvance
  WHERE "Id" = sp_actualizaractividad.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminaractividad(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM actividad a WHERE a."Id" = sp_eliminaractividad.Id) THEN
    RAISE EXCEPTION 'La actividad no existe.';
  END IF;
  DELETE FROM actividad WHERE "Id" = sp_eliminaractividad.Id;
END;
$$;

-- ================= DISTRIBUCIONPRESUPUESTO =================
-- Nota: se reordenó MontoAsignado antes de IdProyectoHijo (Postgres exige que
-- los parámetros con DEFAULT vayan al final del listado).

CREATE OR REPLACE PROCEDURE sp_insertardistribucionpresupuesto(
  IdPresupuestoPadre integer, MontoAsignado numeric(15,2), IdProyectoHijo integer DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_insertardistribucionpresupuesto.IdPresupuestoPadre) THEN
    RAISE EXCEPTION 'El presupuesto padre no existe.';
  END IF;
  IF sp_insertardistribucionpresupuesto.IdProyectoHijo IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM proyecto py WHERE py."Id" = sp_insertardistribucionpresupuesto.IdProyectoHijo) THEN
    RAISE EXCEPTION 'El proyecto hijo no existe.';
  END IF;
  INSERT INTO distribucionpresupuesto ("IdPresupuestoPadre","IdProyectoHijo","MontoAsignado")
  VALUES (sp_insertardistribucionpresupuesto.IdPresupuestoPadre, sp_insertardistribucionpresupuesto.IdProyectoHijo, sp_insertardistribucionpresupuesto.MontoAsignado);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizardistribucionpresupuesto(
  Id integer, IdPresupuestoPadre integer, MontoAsignado numeric(15,2), IdProyectoHijo integer DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM distribucionpresupuesto d WHERE d."Id" = sp_actualizardistribucionpresupuesto.Id) THEN
    RAISE EXCEPTION 'La distribución de presupuesto no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_actualizardistribucionpresupuesto.IdPresupuestoPadre) THEN
    RAISE EXCEPTION 'El presupuesto padre no existe.';
  END IF;
  IF sp_actualizardistribucionpresupuesto.IdProyectoHijo IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM proyecto py WHERE py."Id" = sp_actualizardistribucionpresupuesto.IdProyectoHijo) THEN
    RAISE EXCEPTION 'El proyecto hijo no existe.';
  END IF;
  UPDATE distribucionpresupuesto SET
    "IdPresupuestoPadre" = sp_actualizardistribucionpresupuesto.IdPresupuestoPadre,
    "IdProyectoHijo" = sp_actualizardistribucionpresupuesto.IdProyectoHijo,
    "MontoAsignado" = sp_actualizardistribucionpresupuesto.MontoAsignado
  WHERE "Id" = sp_actualizardistribucionpresupuesto.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminardistribucionpresupuesto(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM distribucionpresupuesto d WHERE d."Id" = sp_eliminardistribucionpresupuesto.Id) THEN
    RAISE EXCEPTION 'La distribución de presupuesto no existe.';
  END IF;
  DELETE FROM distribucionpresupuesto WHERE "Id" = sp_eliminardistribucionpresupuesto.Id;
END;
$$;

-- ================= EJECUCIONPRESUPUESTO =================

CREATE OR REPLACE PROCEDURE sp_insertarejecucionpresupuesto(
  IdPresupuesto integer, Anio integer, MontoPlaneado numeric(15,2) DEFAULT NULL,
  MontoEjecutado numeric(15,2) DEFAULT NULL, Observaciones text DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_insertarejecucionpresupuesto.IdPresupuesto) THEN
    RAISE EXCEPTION 'El Presupuesto no existe.';
  END IF;
  INSERT INTO ejecucionpresupuesto ("IdPresupuesto","Anio","MontoPlaneado","MontoEjecutado","Observaciones")
  VALUES (sp_insertarejecucionpresupuesto.IdPresupuesto, sp_insertarejecucionpresupuesto.Anio, sp_insertarejecucionpresupuesto.MontoPlaneado, sp_insertarejecucionpresupuesto.MontoEjecutado, sp_insertarejecucionpresupuesto.Observaciones);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarejecucionpresupuesto(
  Id integer, IdPresupuesto integer, Anio integer, MontoPlaneado numeric(15,2) DEFAULT NULL,
  MontoEjecutado numeric(15,2) DEFAULT NULL, Observaciones text DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM ejecucionpresupuesto e WHERE e."Id" = sp_actualizarejecucionpresupuesto.Id) THEN
    RAISE EXCEPTION 'La ejecución del presupuesto no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_actualizarejecucionpresupuesto.IdPresupuesto) THEN
    RAISE EXCEPTION 'El Presupuesto no existe.';
  END IF;
  UPDATE ejecucionpresupuesto SET
    "IdPresupuesto" = sp_actualizarejecucionpresupuesto.IdPresupuesto,
    "Anio" = sp_actualizarejecucionpresupuesto.Anio,
    "MontoPlaneado" = sp_actualizarejecucionpresupuesto.MontoPlaneado,
    "MontoEjecutado" = sp_actualizarejecucionpresupuesto.MontoEjecutado,
    "Observaciones" = sp_actualizarejecucionpresupuesto.Observaciones
  WHERE "Id" = sp_actualizarejecucionpresupuesto.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarejecucionpresupuesto(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM ejecucionpresupuesto e WHERE e."Id" = sp_eliminarejecucionpresupuesto.Id) THEN
    RAISE EXCEPTION 'La ejecución del presupuesto no existe.';
  END IF;
  DELETE FROM ejecucionpresupuesto WHERE "Id" = sp_eliminarejecucionpresupuesto.Id;
END;
$$;

-- ================= METAESTRATEGICA =================

CREATE OR REPLACE PROCEDURE sp_insertarmetaestrategica(IdObjetivo integer, Titulo varchar, Descripcion text DEFAULT NULL)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM objetivoestrategico o WHERE o."Id" = sp_insertarmetaestrategica.IdObjetivo) THEN
    RAISE EXCEPTION 'El objetivo estratégico no existe.';
  END IF;
  INSERT INTO metaestrategica ("IdObjetivo","Titulo","Descripcion")
  VALUES (sp_insertarmetaestrategica.IdObjetivo, sp_insertarmetaestrategica.Titulo, sp_insertarmetaestrategica.Descripcion);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarmetaestrategica(Id integer, IdObjetivo integer, Titulo varchar, Descripcion text DEFAULT NULL)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM metaestrategica m WHERE m."Id" = sp_actualizarmetaestrategica.Id) THEN
    RAISE EXCEPTION 'La meta estratégica no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM objetivoestrategico o WHERE o."Id" = sp_actualizarmetaestrategica.IdObjetivo) THEN
    RAISE EXCEPTION 'El objetivo estratégico no existe.';
  END IF;
  UPDATE metaestrategica SET
    "IdObjetivo" = sp_actualizarmetaestrategica.IdObjetivo,
    "Titulo" = sp_actualizarmetaestrategica.Titulo,
    "Descripcion" = sp_actualizarmetaestrategica.Descripcion
  WHERE "Id" = sp_actualizarmetaestrategica.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarmetaestrategica(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM metaestrategica m WHERE m."Id" = sp_eliminarmetaestrategica.Id) THEN
    RAISE EXCEPTION 'La meta estratégica no existe.';
  END IF;
  DELETE FROM metaestrategica WHERE "Id" = sp_eliminarmetaestrategica.Id;
END;
$$;

-- ================= OBJETIVOESTRATEGICO =================

CREATE OR REPLACE PROCEDURE sp_insertarobjetivoestrategico(IdVariable integer, Titulo varchar, Descripcion text DEFAULT NULL)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM variableestrategica v WHERE v."Id" = sp_insertarobjetivoestrategico.IdVariable) THEN
    RAISE EXCEPTION 'La variable estratégica no existe.';
  END IF;
  INSERT INTO objetivoestrategico ("IdVariable","Titulo","Descripcion")
  VALUES (sp_insertarobjetivoestrategico.IdVariable, sp_insertarobjetivoestrategico.Titulo, sp_insertarobjetivoestrategico.Descripcion);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarobjetivoestrategico(Id integer, IdVariable integer, Titulo varchar, Descripcion text DEFAULT NULL)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM objetivoestrategico o WHERE o."Id" = sp_actualizarobjetivoestrategico.Id) THEN
    RAISE EXCEPTION 'El objetivo estratégico no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM variableestrategica v WHERE v."Id" = sp_actualizarobjetivoestrategico.IdVariable) THEN
    RAISE EXCEPTION 'La variable estratégica no existe.';
  END IF;
  UPDATE objetivoestrategico SET
    "IdVariable" = sp_actualizarobjetivoestrategico.IdVariable,
    "Titulo" = sp_actualizarobjetivoestrategico.Titulo,
    "Descripcion" = sp_actualizarobjetivoestrategico.Descripcion
  WHERE "Id" = sp_actualizarobjetivoestrategico.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarobjetivoestrategico(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM objetivoestrategico o WHERE o."Id" = sp_eliminarobjetivoestrategico.Id) THEN
    RAISE EXCEPTION 'El objetivo estratégico no existe.';
  END IF;
  DELETE FROM objetivoestrategico WHERE "Id" = sp_eliminarobjetivoestrategico.Id;
END;
$$;

-- ================= META_PROYECTO =================

CREATE OR REPLACE PROCEDURE sp_insertarmetaproyecto(IdMeta integer, IdProyecto integer, FechaAsociacion date DEFAULT NULL)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM metaestrategica m WHERE m."Id" = sp_insertarmetaproyecto.IdMeta) THEN
    RAISE EXCEPTION 'La meta estratégica no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM proyecto p WHERE p."Id" = sp_insertarmetaproyecto.IdProyecto) THEN
    RAISE EXCEPTION 'El proyecto no existe.';
  END IF;
  IF EXISTS (SELECT 1 FROM meta_proyecto mp WHERE mp."IdMeta" = sp_insertarmetaproyecto.IdMeta AND mp."IdProyecto" = sp_insertarmetaproyecto.IdProyecto) THEN
    RAISE EXCEPTION 'La asociación Meta-Proyecto ya existe.';
  END IF;
  INSERT INTO meta_proyecto ("IdMeta","IdProyecto","FechaAsociacion")
  VALUES (sp_insertarmetaproyecto.IdMeta, sp_insertarmetaproyecto.IdProyecto, sp_insertarmetaproyecto.FechaAsociacion);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarmetaproyecto(IdMeta integer, IdProyecto integer, FechaAsociacion date)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM meta_proyecto mp WHERE mp."IdMeta" = sp_actualizarmetaproyecto.IdMeta AND mp."IdProyecto" = sp_actualizarmetaproyecto.IdProyecto) THEN
    RAISE EXCEPTION 'La asociación Meta-Proyecto no existe.';
  END IF;
  UPDATE meta_proyecto SET "FechaAsociacion" = sp_actualizarmetaproyecto.FechaAsociacion
  WHERE "IdMeta" = sp_actualizarmetaproyecto.IdMeta AND "IdProyecto" = sp_actualizarmetaproyecto.IdProyecto;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarmetaproyecto(IdMeta integer, IdProyecto integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM meta_proyecto mp WHERE mp."IdMeta" = sp_eliminarmetaproyecto.IdMeta AND mp."IdProyecto" = sp_eliminarmetaproyecto.IdProyecto) THEN
    RAISE EXCEPTION 'La asociación Meta-Proyecto no existe.';
  END IF;
  DELETE FROM meta_proyecto WHERE "IdMeta" = sp_eliminarmetaproyecto.IdMeta AND "IdProyecto" = sp_eliminarmetaproyecto.IdProyecto;
END;
$$;

-- ================= PRESUPUESTO =================

CREATE OR REPLACE PROCEDURE sp_insertarpresupuesto(
  IdProyecto integer, MontoSolicitado numeric(15,2), Estado varchar(20) DEFAULT 'Pendiente',
  MontoAprobado numeric(15,2) DEFAULT NULL, PeriodoAnio integer DEFAULT NULL,
  FechaSolicitud date DEFAULT NULL, FechaAprobacion date DEFAULT NULL, Observaciones text DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM proyecto p WHERE p."Id" = sp_insertarpresupuesto.IdProyecto) THEN
    RAISE EXCEPTION 'El proyecto no existe.';
  END IF;
  IF sp_insertarpresupuesto.Estado NOT IN ('Pendiente','Aprobado','Rechazado') THEN
    RAISE EXCEPTION 'Estado inválido. Debe ser Pendiente, Aprobado o Rechazado.';
  END IF;
  INSERT INTO presupuesto ("IdProyecto","MontoSolicitado","Estado","MontoAprobado","PeriodoAnio","FechaSolicitud","FechaAprobacion","Observaciones")
  VALUES (sp_insertarpresupuesto.IdProyecto, sp_insertarpresupuesto.MontoSolicitado, sp_insertarpresupuesto.Estado, sp_insertarpresupuesto.MontoAprobado, sp_insertarpresupuesto.PeriodoAnio, sp_insertarpresupuesto.FechaSolicitud, sp_insertarpresupuesto.FechaAprobacion, sp_insertarpresupuesto.Observaciones);
END;
$$;

CREATE OR REPLACE PROCEDURE sp_actualizarpresupuesto(
  Id integer, IdProyecto integer, MontoSolicitado numeric(15,2), Estado varchar(20),
  MontoAprobado numeric(15,2) DEFAULT NULL, PeriodoAnio integer DEFAULT NULL,
  FechaSolicitud date DEFAULT NULL, FechaAprobacion date DEFAULT NULL, Observaciones text DEFAULT NULL
)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_actualizarpresupuesto.Id) THEN
    RAISE EXCEPTION 'El presupuesto no existe.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM proyecto p WHERE p."Id" = sp_actualizarpresupuesto.IdProyecto) THEN
    RAISE EXCEPTION 'El proyecto no existe.';
  END IF;
  IF sp_actualizarpresupuesto.Estado NOT IN ('Pendiente','Aprobado','Rechazado') THEN
    RAISE EXCEPTION 'Estado inválido. Debe ser Pendiente, Aprobado o Rechazado.';
  END IF;
  UPDATE presupuesto SET
    "IdProyecto" = sp_actualizarpresupuesto.IdProyecto,
    "MontoSolicitado" = sp_actualizarpresupuesto.MontoSolicitado,
    "Estado" = sp_actualizarpresupuesto.Estado,
    "MontoAprobado" = sp_actualizarpresupuesto.MontoAprobado,
    "PeriodoAnio" = sp_actualizarpresupuesto.PeriodoAnio,
    "FechaSolicitud" = sp_actualizarpresupuesto.FechaSolicitud,
    "FechaAprobacion" = sp_actualizarpresupuesto.FechaAprobacion,
    "Observaciones" = sp_actualizarpresupuesto.Observaciones
  WHERE "Id" = sp_actualizarpresupuesto.Id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_eliminarpresupuesto(Id integer)
LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM presupuesto p WHERE p."Id" = sp_eliminarpresupuesto.Id) THEN
    RAISE EXCEPTION 'El presupuesto no existe.';
  END IF;
  DELETE FROM presupuesto WHERE "Id" = sp_eliminarpresupuesto.Id;
END;
$$;
