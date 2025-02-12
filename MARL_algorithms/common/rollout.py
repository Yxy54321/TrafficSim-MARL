import numpy as np
import torch
from torch.distributions import one_hot_categorical
import time


class RolloutWorker:
    def __init__(self, env, agents, args):
        self.env = env
        self.agents = agents
        self.episode_limit = args.episode_limit
        self.n_actions = args.n_actions
        self.n_agents = args.n_agents
        self.state_shape = args.state_shape
        self.obs_shape = args.obs_shape
        self.args = args

        self.epsilon = args.epsilon
        self.anneal_epsilon = args.anneal_epsilon
        self.min_epsilon = args.min_epsilon
        print('Init RolloutWorker')

    @torch.no_grad()
    def generate_episode(self, episode_num=None, evaluate=False):
        if self.args.replay_dir != '' and evaluate and episode_num == 0:  # prepare for save replay of evaluation
            self.env.close()
        o, u, r, s, avail_u, u_onehot, terminate, padded = [], [], [], [], [], [], [], []
        #mod
        last_action = np.zeros((self.args.same, self.args.n_agents, self.args.n_actions))
        if self.args.signal:
            o_sig, u_sig, r_sig, s_sig, avail_u_sig, u_onehot_sig, padded_sig,terminate_sig, padded_sig = [], [], [], [], [], [], [],[],[]
            last_action_sig = np.zeros((1, self.args.signal_args.n_agents, self.args.signal_args.n_actions))

        #shape
        self.env.reset()
        terminated = False
        win_tag = False
        step = 0
        episode_reward = 0  # cumulative rewards


        for ea in self.agents:
            ea.policy.init_hidden(1)

        # epsilon
        epsilon = 0 if evaluate else self.epsilon
        if self.args.epsilon_anneal_scale == 'episode':
            epsilon = epsilon - self.anneal_epsilon if epsilon > self.min_epsilon else epsilon
        print(epsilon)
        # sample z for maven
        if self.args.alg == 'maven':
            obs = self.env.get_obs()
            state = self.env.get_state()
            state = torch.tensor(state, dtype=torch.float32)
            if self.args.cuda:
                state = state.cuda()
            z_prob = self.agents.policy.z_policy(state)
            maven_z = one_hot_categorical.OneHotCategorical(z_prob).sample()
            maven_z = list(maven_z.cpu())

        while not terminated and step < self.episode_limit:
            # time.sleep(0.2)
            signal_obs,obs = self.env.get_obs()
            #目前没有get_state
            state = self.env.get_state()
            actions,actions_sig ,avail_actions, actions_onehot = [], [], [],[]
            for same_agent in range(self.args.same):#1-3
                for agent_id in range(self.n_agents):#1 agent_id
                    #暂无实现get_avail_agent_actions
                    avail_action = self.env.get_avail_agent_actions(agent_id)
                    #忽略第一个if
                    if self.args.alg == 'maven':
                        action = self.agents.choose_action(obs[agent_id], last_action[agent_id], agent_id,
                                                        avail_action, epsilon, maven_z)
                    else:
                        #目前agent id只为0.因为只有一种智能体
                        action = self.agents[same_agent].choose_action(obs[same_agent*self.n_agents+agent_id], last_action[same_agent][agent_id], agent_id,
                                                        avail_action, epsilon)
                    # generate onehot vector of th action
                    action_onehot = np.zeros(self.args.n_actions)
                    action_onehot[action] = 1
                    actions.append(int(action))
                    actions_onehot.append(action_onehot)
                    avail_actions.append(avail_action)
                    last_action[same_agent][agent_id] = action_onehot
            #mod
            if self.args.signal:
                # 获取 obs 的最后 4 列
                obs_part_0 = obs[0][-4:-2]
                obs_part_1 = obs[1][-4:-2]
                signal_observation = []
                for row in range(0, len(signal_obs), 4):
                    # 获取 signal_obs 中每 4 个元素组成一行
                    signal_part = signal_obs[row:row + 4]
                    # 将 obs[0][-4:]、obs[1][-4:] 和 signal_part 拼接成一个新行
                    new_row = np.concatenate((obs_part_0, obs_part_1, signal_part))
                    signal_observation.append(new_row)

                # 转换结果为二维数组
                signal_observation = np.array(signal_observation)

                avail_actions_sig, actions_onehot_sig =  [], []
                for sig_agent_id in range(self.args.signal_args.n_agents):
                    # 暂无实现get_avail_agent_actions
                    avail_action_sig = np.ones(self.args.signal_args.n_actions)
                    # 忽略第一个if

                    action_sig = self.agents[same_agent+1].choose_action(signal_observation[sig_agent_id],
                                                                       last_action_sig[0][sig_agent_id], sig_agent_id,
                                                                       avail_action_sig, epsilon)
                    # generate onehot vector of th action
                    action_onehot_sig = np.zeros(self.args.signal_args.n_actions)
                    action_onehot_sig[action_sig] = 1
                    actions_sig.append(int(action_sig))
                    actions_onehot_sig.append(action_onehot_sig)
                    avail_actions_sig.append(avail_action_sig)
                    last_action_sig[0][sig_agent_id] = action_onehot_sig

            #action来自模型，reward来自环境
            tol_re,reward, terminated, info = self.env.step(actions,actions_sig)
            win_tag = True if terminated and 'battle_won' in info and info['battle_won'] else False
            o.append(obs)
            s.append(state)
            u.append(np.reshape(actions, [self.n_agents * self.args.same, 1]))
            u_onehot.append(actions_onehot)
            avail_u.append(avail_actions)
            reward=np.array(reward)
            r.append(np.reshape(reward, [self.args.same, 1]))
            terminate.append([terminated])
            padded.append([0.])

            #o_sig, u_sig, r_sig, s_sig, avail_u_sig, u_onehot_sig, padded_sig
            if self.args.signal:
                o_sig.append(signal_observation)
                s_sig.append(state)#小问题
                u_sig.append(np.reshape(actions_sig, [self.args.signal_args.n_agents * 1, 1]))
                u_onehot_sig.append(actions_onehot_sig)
                avail_u_sig.append(avail_actions_sig)
                reward_sig = np.repeat(tol_re, self.args.signal_args.n_agents)
                r_sig.append(np.reshape(reward_sig, [self.args.signal_args.n_agents, 1]))
                terminate_sig.append([terminated])
                padded_sig.append([0.])
            # if(isinstance(reward, list)):
            #     episode_reward += sum(reward)
            # else:
            #     episode_reward += reward
            step += 1
            if self.args.epsilon_anneal_scale == 'step':
                epsilon = epsilon - self.anneal_epsilon if epsilon > self.min_epsilon else epsilon

        # last obs
        signal_obs,obs = self.env.get_obs()
        state = self.env.get_state()
        o.append(obs)
        s.append(state)
        o_next = o[1:]
        s_next = s[1:]
        o = o[:-1]
        s = s[:-1]
        # get avail_action for last obs，because target_q needs avail_action in training
        avail_actions = []
        for same_agent in range(self.args.same):
            for agent_id in range(self.n_agents):
                avail_action = self.env.get_avail_agent_actions(agent_id)
                avail_actions.append(avail_action)
        avail_u.append(avail_actions)
        avail_u_next = avail_u[1:]
        avail_u = avail_u[:-1]

        del(r[0])
        tolre,reward, terminated, info = self.env.step(actions,actions_sig)
        reward=np.array(reward)
        r.append(np.reshape(reward, [self.args.same, 1]))

        if self.args.signal:
            obs_part_0 = obs[0][-4:-2]
            obs_part_1 = obs[1][-4:-2]
            signal_observation = []
            for row in range(0, len(signal_obs), 4):
                # 获取 signal_obs 中每 4 个元素组成一行
                signal_part = signal_obs[row:row + 4]
                # 将 obs[0][-4:]、obs[1][-4:] 和 signal_part 拼接成一个新行
                new_row = np.concatenate((obs_part_0, obs_part_1, signal_part))
                signal_observation.append(new_row)
            o_sig.append(signal_observation)
            s_sig.append(state)
            o_next_sig = o_sig[1:]
            s_next_sig = s_sig[1:]
            o_sig = o_sig[:-1]
            s_sig = s_sig[:-1]
            # get avail_action for last obs，because target_q needs avail_action in training
            avail_action_sig = np.ones(self.args.signal_args.n_actions)
            avail_u_sig.append(avail_actions_sig)
            avail_u_next_sig = avail_u_sig[1:]
            avail_u_sig = avail_u_sig[:-1]

            del (r_sig[0])
            reward_sig = np.repeat(tol_re, self.args.signal_args.n_agents)
            r_sig.append(np.reshape(reward_sig, [self.args.signal_args.n_agents, 1]))


        # if step < self.episode_limit，padding
        for i in range(step, self.episode_limit):
            o.append(np.zeros((self.n_agents * self.args.same, self.obs_shape)))
            u.append(np.zeros([self.n_agents * self.args.same, 1]))
            s.append(np.zeros((2,self.state_shape)))
            # r.append([0.])
            r.append(np.zeros([self.args.same, 1]))
            o_next.append(np.zeros((self.n_agents * self.args.same, self.obs_shape)))
            s_next.append(np.zeros((2,self.state_shape)))
            u_onehot.append(np.zeros((self.n_agents * self.args.same, self.n_actions)))
            avail_u.append(np.zeros((self.n_agents * self.args.same, self.n_actions)))
            avail_u_next.append(np.zeros((self.n_agents * self.args.same, self.n_actions)))
            padded.append([1.])
            terminate.append([1.])

            if self.args.signal:
                o_sig.append(np.zeros((self.args.signal_args.n_agents,self.args.signal_args.obs_shape)))
                u_sig.append(np.zeros([self.args.signal_args.n_agents, 1]))
                s_sig.append(np.zeros(self.state_shape))
                # r.append([0.])
                r_sig.append(np.zeros([self.args.signal_args.n_agents, 1]))
                o_next_sig.append(np.zeros((self.args.signal_args.n_agents,self.args.signal_args.obs_shape)))
                s_next_sig.append(np.zeros(22))
                u_onehot_sig.append(np.zeros((self.args.signal_args.n_agents, self.args.signal_args.n_actions)))
                avail_u_sig.append(np.zeros((self.args.signal_args.n_agents,self.args.signal_args.n_actions)))
                avail_u_next_sig.append(np.zeros((self.args.signal_args.n_agents,self.args.signal_args.n_actions)))
                padded_sig.append([1.])
                terminate_sig.append([1.])

        episode = dict(o=o.copy(),
                       s=s.copy(),
                       u=u.copy(),
                       r=r.copy(),
                       avail_u=avail_u.copy(),
                       o_next=o_next.copy(),
                       s_next=s_next.copy(),
                       avail_u_next=avail_u_next.copy(),
                       u_onehot=u_onehot.copy(),
                       padded=padded.copy(),
                       terminated=terminate.copy()
                       )
        # add episode dim
        for key in episode.keys():
            episode[key] = np.array([episode[key]])
        if not evaluate:
            self.epsilon = epsilon
        if self.args.alg == 'maven':
            episode['z'] = np.array([maven_z.copy()])
        if evaluate and episode_num == self.args.evaluate_epoch - 1 and self.args.replay_dir != '':
            self.env.save_replay()
            self.env.close()

        if self.args.signal:
            episode_sig = dict(o_sig=o_sig.copy(),
                           s_sig=s_sig.copy(),
                           u_sig=u_sig.copy(),
                           r_sig=r_sig.copy(),
                           avail_u_sig=avail_u_sig.copy(),
                           o_next_sig=o_next_sig.copy(),
                           s_next_sig=s_next_sig.copy(),
                           avail_u_next_sig=avail_u_next_sig.copy(),
                           u_onehot_sig=u_onehot_sig.copy(),
                           padded_sig=padded_sig.copy(),
                           terminated_sig=terminate_sig.copy()
                           )
            # add episode dim
            for key in episode_sig.keys():
                print(f"episode_sig[{key}] 的形状: {np.shape(episode_sig[key])}")
                episode_sig[key] = np.array([episode_sig[key]])
            if not evaluate:
                self.epsilon = epsilon
            if self.args.alg == 'maven':
                episode_sig['z'] = np.array([maven_z.copy()])
            if evaluate and episode_num == self.args.evaluate_epoch - 1 and self.args.replay_dir != '':
                self.env.save_replay()
                self.env.close()
        if self.args.signal:
            return episode_sig ,episode, episode_reward, win_tag, step
        else:
            return episode, episode_reward, win_tag, step


# RolloutWorker for communication
class CommRolloutWorker:
    def __init__(self, env, agents, args):
        self.env = env
        self.agents = agents
        self.episode_limit = args.episode_limit
        self.n_actions = args.n_actions
        self.n_agents = args.n_agents
        self.state_shape = args.state_shape
        self.obs_shape = args.obs_shape
        self.args = args

        self.epsilon = args.epsilon
        self.anneal_epsilon = args.anneal_epsilon
        self.min_epsilon = args.min_epsilon
        print('Init CommRolloutWorker')

    @torch.no_grad()
    def generate_episode(self, episode_num=None, evaluate=False):
        if self.args.replay_dir != '' and evaluate and episode_num == 0:  # prepare for save replay
            self.env.close()
        o, u, r, s, avail_u, u_onehot, terminate, padded = [], [], [], [], [], [], [], []
        self.env.reset()
        terminated = False
        win_tag = False
        step = 0
        episode_reward = 0
        last_action = np.zeros((self.args.n_agents, self.args.n_actions))
        self.agents.policy.init_hidden(1)
        epsilon = 0 if evaluate else self.epsilon
        if self.args.epsilon_anneal_scale == 'episode':
            epsilon = epsilon - self.anneal_epsilon if epsilon > self.min_epsilon else epsilon
        while not terminated and step < self.episode_limit:
            # time.sleep(0.2)
            obs = self.env.get_obs()
            state = self.env.get_state()
            actions, avail_actions, actions_onehot = [], [], []

            # get the weights of all actions for all agents
            weights = self.agents.get_action_weights(np.array(obs), last_action)

            # choose action for each agent
            for agent_id in range(self.n_agents):
                avail_action = self.env.get_avail_agent_actions(agent_id)
                action = self.agents.choose_action(weights[agent_id], avail_action, epsilon)

                # generate onehot vector of th action
                action_onehot = np.zeros(self.args.n_actions)
                action_onehot[action] = 1
                actions.append(int(action))
                actions_onehot.append(action_onehot)
                avail_actions.append(avail_action)
                last_action[agent_id] = action_onehot

            reward, terminated, info = self.env.step(actions)
            win_tag = True if terminated and 'battle_won' in info and info['battle_won'] else False
            o.append(obs)
            s.append(state)
            u.append(np.reshape(actions, [self.n_agents, 1]))
            u_onehot.append(actions_onehot)
            avail_u.append(avail_actions)
            r.append([reward])
            terminate.append([terminated])
            padded.append([0.])
            episode_reward += reward
            step += 1
            # if terminated:
            #     time.sleep(1)
            if self.args.epsilon_anneal_scale == 'step':
                epsilon = epsilon - self.anneal_epsilon if epsilon > self.min_epsilon else epsilon
        # last obs
        obs = self.env.get_obs()
        state = self.env.get_state()
        o.append(obs)
        s.append(state)
        o_next = o[1:]
        s_next = s[1:]
        o = o[:-1]
        s = s[:-1]
        # get avail_action for last obs，because target_q needs avail_action in training
        avail_actions = []
        for agent_id in range(self.n_agents):
            avail_action = self.env.get_avail_agent_actions(agent_id)
            avail_actions.append(avail_action)
        avail_u.append(avail_actions)
        avail_u_next = avail_u[1:]
        avail_u = avail_u[:-1]
        
        del(r[0])
        reward, terminated, info = self.env.step(actions)
        if(isinstance(reward, list)):
            r.append(reward)
        else:
            r.append([reward])

        # if step < self.episode_limit，padding
        for i in range(step, self.episode_limit):
            o.append(np.zeros((self.n_agents, self.obs_shape)))
            u.append(np.zeros([self.n_agents, 1]))
            s.append(np.zeros(self.state_shape))
            r.append([0.])
            o_next.append(np.zeros((self.n_agents, self.obs_shape)))
            s_next.append(np.zeros(self.state_shape))
            u_onehot.append(np.zeros((self.n_agents, self.n_actions)))
            avail_u.append(np.zeros((self.n_agents, self.n_actions)))
            avail_u_next.append(np.zeros((self.n_agents, self.n_actions)))
            padded.append([1.])
            terminate.append([1.])

        episode = dict(o=o.copy(),
                       s=s.copy(),
                       u=u.copy(),
                       r=r.copy(),
                       avail_u=avail_u.copy(),
                       o_next=o_next.copy(),
                       s_next=s_next.copy(),
                       avail_u_next=avail_u_next.copy(),
                       u_onehot=u_onehot.copy(),
                       padded=padded.copy(),
                       terminated=terminate.copy()
                       )
        # add episode dim
        for key in episode.keys():
            episode[key] = np.array([episode[key]])
        if not evaluate:
            self.epsilon = epsilon
            # print('Epsilon is ', self.epsilon)
        if evaluate and episode_num == self.args.evaluate_epoch - 1 and self.args.replay_dir != '':
            self.env.save_replay()
            self.env.close()
        return episode, episode_reward, win_tag, step
