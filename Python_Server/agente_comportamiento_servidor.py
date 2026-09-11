
from mesa import Agent, Model
from http.server import BaseHTTPRequestHandler, HTTPServer
import logging
import json
import random

import heapq


class GameState:
    def __init__(self, data: dict):
      #Datos de cada turno
      self.turn = data.get("turn")
      self.phase = data.get("phase")
      self.current_agent_id = data.get("current_agent")
      self.game_status = data.get("game_status")

      #Tablero
      self.grid = data.get("grid")

      # Entidades
      self.poi = data.get("poi")
      self.fire = [tuple(p) for p in (data.get("fire") or [])]
      self.smoke = [tuple(p) for p in (data.get("smoke") or [])]
      self.doors = data.get("doors")
      self.damaged_walls = data.get("walls")
      self.entrances = data.get("entrances")
      #Agentes
      self.agents = data.get("agents")

      #Marcador
      self.stats = data.get("game")

    # Agente Actual
    def my_agent(self) -> dict:
      for agent in self.agents:
        if agent["id"] == self.current_agent_id:
          return agent

      return None

    #Posicion Agente
    def get_position(self, agent_id = None):
      if agent_id is None:
        agent_id = self.current_agent_id

      for agent in self.agents:
        if agent["id"] == agent_id:
          return agent["position"]

      return None

    #Tablero/Obstaculos
    def cell_walls(self,position: tuple) -> str:
      if not self.in_bounds(position):
        return None

      x, y = position
      return self.grid[y - 1][x - 1]

    def door_between(self, position1: tuple, position2: tuple) -> dict:
      for door in self.doors:
        between = door["between"]

        p1 = tuple(between[0])
        p2 = tuple(between[1])

        if (p1 == position1 and p2 == position2) or (p1 == position2 and p2 == position1):
          return door

      return None

    def damaged_wall_between(self, position1: tuple, position2: tuple) -> dict:
      for wall in self.damaged_walls:
        between = wall["between"]

        p1 = tuple(between[0])
        p2 = tuple(between[1])

        if (p1 == position1 and p2 == position2) or (p1 == position2 and p2 == position1):
          return wall

      return None

    def wall_between(self, position1: tuple, position2: tuple):
      x1, y1 = position1
      x2, y2 = position2

      cell = self.cell_walls(position1)

      if cell is None:
        return None

      if y2 == y1 - 1:
        return cell[0] == "1"

      if x2 == x1 - 1:
        return cell[1] == "1"

      if y2 == y1 + 1:
        return cell[2] == "1"

      if x2 == x1 + 1:
        return cell[3] == "1"

      return False

    def destroy_wall(self, position1: tuple, position2: tuple):
      x1, y1 = position1
      x2, y2 = position2

      cell1 = self.cell_walls(position1)
      cell2 = self.cell_walls(position2)

      if cell1 is None or cell2 is None:
        return False

      cell1 = list(cell1)
      cell2 = list(cell2)

      if y2 == y1 - 1:
          cell1[0] = "0"
          cell2[2] = "0"

      elif x2 == x1 - 1:
          cell1[1] = "0"
          cell2[3] = "0"

      elif y2 == y1 + 1:
          cell1[2] = "0"
          cell2[0] = "0"

      elif x2 == x1 + 1:
          cell1[3] = "0"
          cell2[1] = "0"

      else:
          return False

      self.grid[y1 - 1][x1 - 1] = "".join(cell1)
      self.grid[y2 - 1][x2 - 1] = "".join(cell2)

      return True

    def in_bounds(self, position: tuple) -> bool:
      x, y = position
      return 1 <= x <= len(self.grid[0]) and 1 <= y <= len(self.grid)

#--Fuego y Humo
    def is_fire(self, pos) -> bool:
      return pos in self.fire

    def is_smoke(self, pos) -> bool:
      return pos in self.smoke

    def get_fire_targets(self):
      return self.fire

    def get_smoke_targets(self):
      return self.smoke

    #POIs
    def visible_pois(self, poi):
      if poi is None:
        return None

      if poi["status"] == "known":
        return {
            "id": poi["id"],
            "position": poi["position"],
            "status": poi["status"],
            "result": poi["result"]
        }

      return {
          "id": poi["id"],
          "position": poi["position"],
          "status": poi["status"]
      }

    def poi_at(self, position):
      position = tuple(position)

      for poi in self.poi:
        if tuple(poi["position"]) == position:
          return self.visible_pois(poi)

      return None

    def get_unknown_poi(self):
      pois = []

      for poi in self.poi:
        if poi["status"] == "unknown":
          pois.append({
              "id": poi["id"],
              "position": poi["position"],
              "status": poi["status"]
          })

      return pois

    def get_known_victims(self):
      victims = []

      for poi in self.poi:
        if poi["status"] == "known":
          if poi["result"] == "victim":
            victims.append(poi)

      return victims

    def reveal_poi(self, poi_id):
      for poi in self.poi:
          if poi["id"] == poi_id:
              poi["status"] = "known"
              return poi

      return None

    def pickup_poi(self, poi_id):
      for poi in self.poi:
          if poi["id"] == poi_id:
              self.poi.remove(poi)
              return True

      return False

    # Neighbors
    def get_neighbors(self, position):
      x, y = position
      posible = [(x, y - 1), (x - 1, y), (x, y + 1), (x + 1, y)]
      neighbors = []
      for pos in posible:
        if self.in_bounds(pos):
          neighbors.append(pos)
      return neighbors

class PriorityQueue:
    def __init__(self):
        self.__data = []

    # Función para verificar si la fila de prioridades está vacía
    def empty(self):
        return not self.__data

    # Función para limpiar la fila de prioridades
    def clear(self):
        self.__data.clear()

    # Función para insertar un elemento en la fila de prioridades
    def push(self, priority, value):
        heapq.heappush(self.__data, (priority, value))

    # Función para extraer el elemento con mayor prioridad (menor número)
    def pop(self):
        if self.__data: # not empty
            return heapq.heappop(self.__data)
        else:
            raise Exception("No such element")

    # Función para obtener el primer elemento sin sacarlo
    def top(self):
        if self.__data: # not empty
            return self.__data[0]
        else:
            raise Exception("No such element")

class Pathfinder:
    COST_MOVE = 1
    COST_MOVE_FIRE = 2
    COST_SMOKE = 1
    COST_OPEN_DOOR = 1
    COST_DAMAGE_WALL = 2
    COST_CARRY_VICTIM = 2
    COST_EXTINGUISH_FIRE = 1
    COST_CLEAR_SMOKE = 1

    BLOCKED = float("inf")
    def __init__(self, state: GameState):
        self.state = state

    def heuristica(self, pos, goal):
      return (abs(pos[0] - goal[0]) + abs(pos[1] - goal[1]) )* self.COST_MOVE

    def costo_celda(self, pos, cargando_victima = False):
      # No entrar a fuego con victima
      if cargando_victima and self.state.is_fire(pos):
        return self.BLOCKED

      # Moverse con victima cuesta 2 AP
      if cargando_victima:
        return self.COST_CARRY_VICTIM

      # Moverse a fuego cuesta 2 AP
      if self.state.is_fire(pos):
        return self.COST_MOVE_FIRE

      # Moverse a humo cuesta 1 AP
      if self.state.is_smoke(pos):
        return self.COST_SMOKE

      # Moverse a celda vacia cuesta 1 AP
      return self.COST_MOVE

    def costo_borde(self, u, v, cargando_victima = False):
      costo_celda = self.costo_celda(v, cargando_victima)

      # Si destino bloqueado
      if costo_celda == self.BLOCKED:
        return self.BLOCKED

      door = self.state.door_between(u,v)

      if door:
        if door["status"] in ["open", "destroyed"]:
          return self.costo_celda(v, cargando_victima)

        return self.COST_OPEN_DOOR + self.costo_celda(v, cargando_victima)

      wall = self.state.damaged_wall_between(u,v)

      if wall:
        return self.COST_DAMAGE_WALL + self.costo_celda(v, cargando_victima)

      if self.state.wall_between(u,v):
        return (2*self.COST_DAMAGE_WALL) + self.costo_celda(v, cargando_victima)

      return self.costo_celda(v, cargando_victima)

    def a_estrella(self, start, goal, cargando_victima = False):
      dist = {start:0}
      prev = {}

      prioridad = self.heuristica(start,goal)

      pq = PriorityQueue()
      pq.push(prioridad, start)
      steps = 0

      while not pq.empty():
        priority, u = pq.pop()

        if u == goal:
          break

        for v in self.state.get_neighbors(u):
          cost = self.costo_borde(u, v, cargando_victima)
          if cost >= self.BLOCKED:
            continue

          new_dist = dist[u] + cost

          if new_dist < dist.get(v, self.BLOCKED):
            dist[v] = new_dist
            prev[v] = u
            priority = new_dist + self.heuristica(v,goal)
            pq.push(priority, v)
        steps += 1

      path = []
      u = goal
      if prev.get(u) is not None or u == start:
        while u is not None:
          path.insert(0,u)
          u = prev.get(u)

      return (dist.get(goal, self.BLOCKED), path, steps)

    def find_path(self, start, goal, cargando_victima = False):
      return self.a_estrella(start,goal, cargando_victima)

class ReservationManager:
    def __init__(self):
        self.reservations = {}

    def is_reserved(self, target_type, target_id, by_agent=None):
        key = (target_type, target_id)
        if key not in self.reservations:
            return False

        owner = self.reservations[key]

        #Se considera no reservado si lo reservo el que consulta
        if owner != None:
          return owner != by_agent

        return True

    def has_reservation(self, agent_id = None):
      for key, owner in self.reservations.items():
        if owner == agent_id:
          return key

      return None

    def reserve(self, target_type, target_id, agent_id):
        key = (target_type, target_id)
        if key in self.reservations:
            return False

        self.reservations[key] = agent_id
        return True

    def release(self, target_type, target_id):
        key = (target_type, target_id)
        if key in self.reservations:
            del self.reservations[key]

class ActionBuilder:
    def __init__(self, agent_id, starting_ap, state):
        self.state = state
        self.agent_id = agent_id
        self.remaining_ap = starting_ap
        self.actions = []

    def puede_pagar(self, cost):
        return cost <= self.remaining_ap

    def fire_to_smoke(self, position, cost = 1):
        if not self.puede_pagar(cost):
            return False

        if position not in self.state.fire:
            return False

        self.remaining_ap -= cost

        self.actions.append({
            "order": len(self.actions) + 1,
            "type": "extinguish_fire",
            "position": list(position),
            "cost": cost,
            "remaining_ap": self.remaining_ap
        })

        self.state.fire.remove(position)

        if position not in self.state.smoke:
            self.state.smoke.append(position)

        return True

    def move(self, from_pos, to_pos, cost=1):
        if cost > self.remaining_ap:
            return False

        self.remaining_ap -= cost
        self.actions.append({
            "order": len(self.actions) + 1,
            "type": "move",
            "from": from_pos,
            "to": to_pos,
            "cost": cost,
            "remaining_ap": self.remaining_ap
        })

        return True

    def open_door(self,between, cost = 1):
      if not self.puede_pagar(cost):
        return False

      self.remaining_ap -= cost
      self.actions.append({
          "order": len(self.actions) + 1,
          "id": self.state.door_between(between[0], between[1])["id"],
          "type": "open_door",
          "between": [list(between[0]), list(between[1])],
          "cost": cost,
          "remaining_ap": self.remaining_ap
      })

      door = self.state.door_between(between[0], between[1])
      if door:
          door["status"] = "open"

      return True

    def clear_smoke(self, position, cost = 1):
      if not self.puede_pagar(cost):
        return False
      self.remaining_ap -= cost
      self.actions.append({
          "order": len(self.actions) + 1,
          "type": "clear_smoke",
          "position": list(position),
          "cost": cost,
          "remaining_ap": self.remaining_ap
      })

      if position in self.state.smoke:
          self.state.smoke.remove(position)

      return True

    def damage_wall(self, between, cost = 2):
      if not self.puede_pagar(cost):
        return False
      self.remaining_ap -= cost
      self.actions.append({
          "order": len(self.actions) + 1,
          "type": "damage_Wall",
          "between": [list(between[0]), list(between[1])],
          "cost": cost,
          "remaining_ap": self.remaining_ap
      })

      # Revisa si se tenia un punto de daño
      damaged_wall = self.state.damaged_wall_between(between[0], between[1])

      # Elimina de damaged_walls al recibir un segundo daño
      if damaged_wall:
        self.state.damaged_walls.remove(damaged_wall)
        self.state.destroy_wall(between[0], between[1])

      else:
        # Agrega a damaged_walls al ser su primer daño
        self.state.damaged_walls.append({
            "id": len(self.state.damaged_walls) + random.randint(100, 1000),
            "between": [list(between[0]), list(between[1])]
        })

      return True

    def pickup_victim(self, poi_id, position):
      if not self.state.pickup_poi(poi_id):
          return False
      self.actions.append({
          "order": len(self.actions) + 1,
          "type": "pickup_victim",
          "poi_id": poi_id,
          "position": list(position),
          "cost": 0,
          "remaining_ap": self.remaining_ap
      })

      self.state.my_agent()["carrying_victim"] = True
      return True

    def move_with_victim(self, from_pos, to_pos, cost = 2):
      if not self.puede_pagar(cost):
        return False
      self.remaining_ap -= cost
      self.actions.append({
          "order": len(self.actions) + 1,
          "type": "move_with_victim",
          "from": from_pos,
          "to": to_pos,
          "cost": cost,
          "remaining_ap": self.remaining_ap
      })

      return True

    def reveal_poi(self, poi_id, position):
      self.actions.append({
          "order": len(self.actions) + 1,
          "type": "reveal_poi",
          "poi_id": poi_id,
          "position": list(position),
          "cost": 0,
          "remaining_ap": self.remaining_ap
      })

      return True

    def rescue_victim(self, position):
        self.actions.append({
            "order": len(self.actions) + 1,
            "type": "rescue_victim",
            "position": list(position),
            "cost": 0,
            "remaining_ap": self.remaining_ap
        })

        self.state.my_agent()["carrying_victim"] = False

        return True

    def build(self):
      return {"agent_id": self.agent_id, "actions": self.actions}

class Bombero(Agent):
    AP = 4

    def __init__(self, model, state, pathfinder, reservations):
        super().__init__(model)
        self.state = state
        self.pathfinder = pathfinder
        self.reservations = reservations
        self.pos_actual = tuple(state.my_agent()["position"])
        self.cargando_victima = state.my_agent()["carrying_victim"]
        self.builder = ActionBuilder(agent_id = state.my_agent()["id"], starting_ap = state.my_agent()["ap"], state = state)

    def sensor_celda_actual(self):
        celda_actual = {
            "poi": self.state.poi_at(self.pos_actual),
            "fire": self.state.is_fire(self.pos_actual),
            "smoke": self.state.is_smoke(self.pos_actual)
        }
        return celda_actual

    def sensor_vecindario(self):
        vecinos = []

        for pos in self.state.get_neighbors(self.pos_actual):
            vecinos.append({
                "position": pos,
                "poi": self.state.poi_at(pos),
                "fire": self.state.is_fire(pos),
                "smoke": self.state.is_smoke(pos),
                "door": self.state.door_between(self.pos_actual, pos),
                "damaged_wall": self.state.damaged_wall_between(self.pos_actual, pos)
            })

        return vecinos

    def puede_actuar_sobre(self, posicion):
        # Debe estar en una las posiciones adyacentes
        if posicion not in self.state.get_neighbors(self.pos_actual):
            return False

        # Puerta cerrada bloquea accion
        door = self.state.door_between(self.pos_actual, posicion)
        if door and door["status"] == "closed":
            return False

        # Pared con daño bloquea accion
        damaged_wall = self.state.damaged_wall_between(self.pos_actual, posicion)
        if damaged_wall:
            return False

        # Pared intacta bloquea accion
        if self.state.wall_between(self.pos_actual, posicion):
            return False

        return True

    def puede_atravesar_fuego(self, siguiente):
        # Con victima no se puede entrar a fuego
        if self.cargando_victima:
            return False

        # Si la siguiente celda no tiene fuego no hay problema
        if not self.state.is_fire(siguiente):
            return True

        # Costo para entrar a celda con fuego (teniendo en cuenta pared/puerta)
        costo_entrada = self.pathfinder.costo_borde(self.pos_actual, siguiente, cargando_victima = False)

        if self.pathfinder.BLOCKED <= costo_entrada:
            return False

        if self.builder.remaining_ap < costo_entrada:
            return False

        ap_despues = self.builder.remaining_ap - costo_entrada

        for salida in self.state.get_neighbors(siguiente):
            if self.state.is_fire(salida):
                continue

            costo_salida = self.pathfinder.costo_borde(siguiente, salida, cargando_victima = False)

            if costo_salida <= ap_despues:
                return True

        return False

    def _salir_de_fuego(self):
      opciones = []

      for vecino in self.state.get_neighbors(self.pos_actual):
          # No salir a celda con fuego
          if self.state.is_fire(vecino):
              continue

          costo = self.pathfinder.costo_borde(self.pos_actual, vecino, cargando_victima = False)

          if costo <= self.builder.remaining_ap:
              opciones.append((costo, vecino))

      if not opciones:
          return False

      # Celda de menor costo (teniendo en cuenta puertas/paredes)
      costo, vecino = min(opciones, key = lambda x: x[0])

      return self._cruzar_a(vecino)

    def decide(self):
        while self.builder.remaining_ap > 0:
            if not self._siguiente_accion():
                break
        return self.builder.build()

    def _siguiente_accion(self):
        if self.cargando_victima:
            return self._avanzar_a_salida()

        if self.state.is_fire(self.pos_actual):
            return self._salir_de_fuego()

        poi_aqui = self.sensor_celda_actual()["poi"]

        if poi_aqui:
            if poi_aqui["status"] == "unknown":
                poi_real = self.state.reveal_poi(poi_aqui["id"])
                self.builder.reveal_poi(poi_real["id"], poi_real["position"])
            else:
                poi_real = poi_aqui

            if poi_real["result"] == "victim":
                return self._recoger_aqui(poi_real)

            if poi_real["result"] == "false_alarm":
                self.reservations.release("poi", poi_real["id"])
                return False

        for fire in self.state.get_fire_targets():
            if self.puede_actuar_sobre(fire):
              exito = self._atender_fuego(fire)
              if exito:
                  self.reservations.release("fire", tuple(fire))
                  self.reservations.reserve("smoke", tuple(fire), self.state.my_agent()["id"])

              return exito

        for smoke in self.state.get_smoke_targets():
            if self.puede_actuar_sobre(smoke):
                exito = self._atender_humo(smoke)
                if exito:
                    self.reservations.release("smoke", tuple(smoke))

                return exito

        objetivo = self._elegir_objetivo()
        if objetivo is None:
            return False

        return self._avanzar_hacia(tuple(objetivo))

    def _elegir_objetivo(self):
        agent_id = self.state.my_agent()["id"]

        # Revisar si se tiene reserva existente
        reserva = self.reservations.has_reservation(agent_id)
        if reserva:
            target_type, target_id = reserva
            if target_type == "poi":
                for poi in self.state.poi:
                    if poi["id"] == target_id:
                        return poi["position"]

            elif target_type == "fire":
                target = tuple(target_id)
                if target in self.state.get_fire_targets():
                    return target
                self.reservations.release("fire", target)

            elif target_type == "smoke":
                target = tuple(target_id)
                if target in self.state.get_smoke_targets():
                    return target
                self.reservations.release("smoke", target)

        # Victimas conocidas
        for poi in self.state.get_known_victims():
            if not self.reservations.is_reserved("poi", poi["id"], by_agent = agent_id):
                self.reservations.reserve("poi", poi["id"], agent_id)
                return poi["position"]

        # POIs desconocidos
        for poi in self.state.get_unknown_poi():
            if not self.reservations.is_reserved("poi", poi["id"], by_agent = agent_id):
                self.reservations.reserve("poi", poi["id"], agent_id)
                return poi["position"]

        # Fuego
        close_fires = sorted(
            self.state.get_fire_targets(),
            key=lambda e: self.pathfinder.a_estrella(self.pos_actual, tuple(e))[0]
         )
        for fire in close_fires:
            if not self.reservations.is_reserved("fire", tuple(fire), by_agent = agent_id):
                    self.reservations.reserve("fire", tuple(fire), agent_id)
                    return fire

        # Humo
        close_smokes = sorted(
            self.state.get_smoke_targets(),
            key=lambda e: self.pathfinder.a_estrella(self.pos_actual, tuple(e))[0]
        )
        for smoke in close_smokes:
            if not self.reservations.is_reserved("smoke", tuple(smoke), by_agent = agent_id):
                self.reservations.reserve("smoke", tuple(smoke), agent_id)
                return smoke

        return None

    def _avanzar_hacia(self, goal):
        cost, path, steps = self.pathfinder.find_path(self.pos_actual, goal, self.cargando_victima)

        if cost == self.pathfinder.BLOCKED:
            return False

        if len(path) < 2:
            return False

        return self._cruzar_a(path[1])

    def _cruzar_a(self, siguiente):
        # No entrar a fuego llevando una victima
        if self.cargando_victima and self.state.is_fire(siguiente):
            return False

        # Solo entrar a fuego si despues se puede salir
        if not self.cargando_victima and self.state.is_fire(siguiente):
            if not self.puede_atravesar_fuego(siguiente):
                return False

        between = [self.pos_actual, siguiente]

        # Puerta
        door = self.state.door_between(self.pos_actual, siguiente)
        if door:
            if door["status"] == "closed":
                return self.builder.open_door(between, cost = self.pathfinder.COST_OPEN_DOOR)

        # Pared dañada
        damaged_wall = self.state.damaged_wall_between(self.pos_actual, siguiente)
        if damaged_wall:
            return self.builder.damage_wall(between, cost = self.pathfinder.COST_DAMAGE_WALL)

        # Pared Intacta
        wall = self.state.wall_between(self.pos_actual, siguiente)
        if wall:
            return self.builder.damage_wall(between, cost = self.pathfinder.COST_DAMAGE_WALL)

        # Moverse celda con victima
        if self.cargando_victima:
            exito = self.builder.move_with_victim(self.pos_actual, siguiente, cost = self.pathfinder.COST_CARRY_VICTIM)
            if exito:
                self.pos_actual = siguiente
            return exito

        # Moverse celda normal
        cost = self.pathfinder.costo_celda(siguiente, self.cargando_victima)

        if not self.builder.puede_pagar(cost):
            return False

        exito = self.builder.move(self.pos_actual, siguiente, cost)

        if exito:
            self.pos_actual = siguiente

        return exito

    def _recoger_aqui(self, poi):
        exito = self.builder.pickup_victim(poi["id"], self.pos_actual)
        if exito:
            self.cargando_victima = True
            self.reservations.release("poi", poi["id"])
        return exito

    def _avanzar_a_salida(self):
        salidas_posibles = []

        for entrada in self.state.entrances:
            cost, path, steps = self.pathfinder.find_path(self.pos_actual, tuple(entrada), self.cargando_victima)
            if cost < self.pathfinder.BLOCKED:
                salidas_posibles.append((cost, path))

        if not salidas_posibles:
            return False

        costo_salida, camino_salida = min(salidas_posibles, key = lambda x: x[0])

        if self.pos_actual == tuple(camino_salida[-1]):
            self.cargando_victima = False
            return self.builder.rescue_victim(self.pos_actual)

        return self._avanzar_hacia(tuple(camino_salida[-1]))

    def _atender_fuego(self, position):
        return self.builder.fire_to_smoke(position, cost = self.pathfinder.COST_EXTINGUISH_FIRE)

    def _atender_humo(self, position):
        return self.builder.clear_smoke(position, cost = self.pathfinder.COST_CLEAR_SMOKE)

    def step(self):
        return self.decide()

class FlashpointModel(Model):
    def __init__(self):
        super().__init__()
        self.bomberos = {}

    def get_or_create_bombero(self, agent_id, state, pathfinder, reservations):
        if agent_id not in self.bomberos:
            bombero = Bombero(model = self, state = state, pathfinder = pathfinder, reservations = reservations)
            self.bomberos[agent_id] = bombero
        else:
            bombero = self.bomberos[agent_id]
            bombero.state = state
            bombero.pathfinder = pathfinder
            bombero.pos_actual = tuple(state.my_agent()["position"])
            bombero.cargando_victima = state.my_agent()["carrying_victim"]
            bombero.builder = ActionBuilder(agent_id = agent_id, starting_ap = state.my_agent()["ap"], state = state)
        return bombero

class CommunicationHandler:
    def __init__(self):
        self.modelo = FlashpointModel()
        self.reservations = ReservationManager()

    def process_turn(self, unity_json: dict) -> dict:
        state = GameState(unity_json)
        pathfinder = Pathfinder(state)

        if state.game_status != "playing":
            return {
                "agent_id": state.current_agent_id,
                "actions": []
                }

        agent_data = state.my_agent()
        if agent_data is None:
            return {
                "agent_id": state.current_agent_id,
                "actions": []
            }
        bombero = self.modelo.get_or_create_bombero(state.current_agent_id, state, pathfinder, self.reservations)

        return bombero.decide()

class Server(BaseHTTPRequestHandler):
    handler = CommunicationHandler()  # se crea UNA sola vez, compartida entre peticiones

    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json; charset=utf-8')
        self.end_headers()

    def do_GET(self):
        self._set_response()
        self.wfile.write("GET request for {}".format(self.path).encode('utf-8'))

    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        body = self.rfile.read(content_length)

        try:
            unity_json = json.loads(body)
            resultado = Server.handler.process_turn(unity_json)

        except Exception as e:
            resultado = {"error": str(e)}

        self._set_response()
        self.wfile.write(json.dumps(resultado).encode('utf-8'))


def run(server_class=HTTPServer, handler_class=Server, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info("Starting httpd...\n")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    logging.info("Stopping httpd...\n")


if __name__ == '__main__':
    from sys import argv
    if len(argv) == 2:
        run(port=int(argv[1]))
    else:
        run()
