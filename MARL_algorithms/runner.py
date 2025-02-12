import numpy as np
import os
from common.rollout import RolloutWorker, CommRolloutWorker
from agent.agent import Agents, CommAgents
from common.replay_buffer import ReplayBuffer
import matplotlib.pyplot as plt
import json
import torch
import random
import threading

class Runner:
    def __init__(self, env, args):
        self.env = env
        #mod
        if args.signal:
            self.signal_agent=Agents(args.signal_args,'signal')
            self.signal_para_path="./model/iql/smart_city/signal_rnn_net_params.pkl" #todo mod

        if args.alg.find('commnet') > -1 or args.alg.find('g2anet') > -1:  # communication agent
            self.agents = CommAgents(args)
            self.rolloutWorker = CommRolloutWorker(env, self.agents, args)
        else:  # no communication agent
            #仍然初始化了3个？
            self.agents=[]
            for agent_i in range(args.same):
                self.agents.append(Agents(args,'car'))
            #mod
            if args.signal:
                self.agents.append(self.signal_agent)
            self.rolloutWorker = RolloutWorker(env, self.agents, args)
        #非on-policy算法，创建两个replay buffer
        if not args.evaluate and args.alg.find('coma') == -1 and args.alg.find('central_v') == -1 and args.alg.find('reinforce') == -1:  # these 3 algorithms are on-poliy
            self.buffer = ReplayBuffer(args)
            self.buffer2 = ReplayBuffer(args)
            #mod
            if args.signal:
                self.signal_buffer = ReplayBuffer(args.signal_args)

        self.args = args
        self.win_rates = []
        self.episode_rewards = []

        # 用来保存plt和pkl
        # plt和pkl分别是什么？
        self.save_path = self.args.result_dir + '/' + args.alg + '/' + args.map
        if not os.path.exists(self.save_path):
            os.makedirs(self.save_path)
        # if os.path.exists('result\iql\smart_city\data.json'):
        #     with open('result\iql\smart_city\data.json', 'r') as f:
        #         # 逐行读取文件内容
        #         for line in f:
        #             all_reward=0
        #             # 解析 JSON 字符串为字典对象
        #             data = json.loads(line)
        #             for key in data.keys():
        #                 data[key] = np.array(data[key])
        #             for idx, er in enumerate(data['r'][0]):
        #                 all_reward += er[0]
        #             if all_reward>0.2:
        #                 self.buffer.store_episode(data)
        #     print("success to load the data")
        
            
    def worker(self, agent, mini_batch, train_steps):
        agent.train(mini_batch, train_steps)
    def split_episode(self, episode):
        split_keys = {'o', 'u','r','s','s_next', 'o_next', 'u_onehot', 'avail_u', 'avail_u_next'}
        #split_keys = {'o', 'u','r', 'o_next', 'u_onehot', 'avail_u', 'avail_u_next'}  # 需要拆分的键
        # 需要拆分的键
        split_episodes = {}
        
        for key, val in episode.items():
            if key in split_keys:
                split_episodes[key] = np.split(val, self.args.same, axis=2)
            else:
                split_episodes[key] = [val] * self.args.same  # 直接复制，不拆分
        
        # 创建一个包含多个子 episode 的列表
        return [dict(zip(split_episodes.keys(), sublist)) for sublist in zip(*split_episodes.values())]

    def run(self, num):

        j=0
        for agent in self.agents:
            if j % 2 == 0:
                path_rnn = './model/iql/smart_city/rnn_net_params.pkl'
            else:
                path_rnn = './model/iql/smart_city/rnn_net_params1.pkl'

            map_location = 'cuda:0' if self.args.cuda else 'cpu'

            # 检查模型文件是否存在
            if os.path.exists(path_rnn):
                print(f"Loading pre-trained model from {path_rnn}")
                agent.policy.eval_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
                agent.policy.target_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
            else:
                print(f"No pre-trained model found at {path_rnn}. Starting from scratch.")

        #mod
        if self.args.signal:
            if os.path.exists(self.signal_para_path):
                print(f"Loading pre-trained model from {self.signal_para_path}")
                agent.policy.eval_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
                agent.policy.target_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
            else:
                print(f"No pre-trained model found at {self.signal_para_path}. Starting from scratch.")


        # @yiiii
        # for agent in self.agents:
        #     if j%2==0:
        #         path_rnn = './model/iql/smart_city/rnn_net_params.pkl'
        #         map_location = 'cuda:0' if self.args.cuda else 'cpu'
        #         agent.policy.eval_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
        #         agent.policy.target_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
        #     else:
        #         path_rnn = './model/iql/smart_city/rnn_net_params1.pkl'
        #         map_location = 'cuda:0' if self.args.cuda else 'cpu'
        #         agent.policy.eval_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))
        #         agent.policy.target_rnn.load_state_dict(torch.load(path_rnn, map_location=map_location))


        #time_steps, train_steps, evaluate_steps什么含义？
        time_steps, train_steps, evaluate_steps = 0, 0, 1
        while time_steps < self.args.n_steps: #self.args.n_steps=160000
            print('Run {}, time_steps {}, train_steps {}'.format(num, time_steps, train_steps))

            if self.args.reuse_one==True:
                j=0
            episodes = []
            tmp=[]
            rm=[]
            # 收集self.args.n_episodes个episodes
            print("n_epsodes {} evaluate_cycle {}".format(self.args.n_episodes, self.args.evaluate_cycle))
            for episode_idx in range(self.args.n_episodes):#todo n_episodes=1,所以目前只有一轮
                if self.args.signal:
                    episode_sig,episode, _, _, steps = self.rolloutWorker.generate_episode(episode_idx)
                else:
                    episode, _, _, steps = self.rolloutWorker.generate_episode(episode_idx)
                tmp=self.split_episode(episode)
                # episodes.append(episode)?
                time_steps += steps
                print("1111")
                print(steps)
            for ea in tmp:#ea这里是一个same_agent的所有数据
                all_reward=0
                # 获取 ea['r'][0] 的形状
                original_shape = ea['r'].shape
                # 创建一个新的数组来存储调整后的数组
                adjusted_r = np.empty(original_shape[:-2] + original_shape[-1:])

                for idx, er in enumerate(ea['r'][0]):
                    all_reward += er[0][0]
                    adjusted_r[0][idx] = er.reshape(-1)  # 对数组进行展平，并存储到新的数组中
                # 将调整后的数组存储回 ea['r'] 中
                ea['r'] = adjusted_r
                rm.append(all_reward)
            sorted_indices = np.argsort(rm)
            half=int(self.args.same/5)
            top=int(self.args.same/10)
            top_10_indices = sorted_indices[-top:]
            top_20_indices=sorted_indices[-half:]
            #一个episode里的最高reward存入episode_reward
            episode_reward= rm[top_10_indices[top-1]]
            win_rate=sum(rm)

        #记录这条episode模型的表现
            num2 = str(evaluate_steps)
            model_dir=self.args.model_dir + '/' + self.args.alg + '/' + self.args.map
            if not os.path.exists(model_dir):
                os.makedirs(model_dir)

            #存储所有agent每个episode的参数
            # for i, agent in enumerate(self.agents):
            #     torch.save(agent.policy.eval_rnn.state_dict(), f"{model_dir}/{num2}_rnn_net_params_agent_{i}.pkl")
            torch.save(self.agents[0].policy.eval_rnn.state_dict(),  model_dir + '/' + num2 + '_rnn_net_params.pkl')
            torch.save(self.agents[1].policy.eval_rnn.state_dict(),  model_dir + '/' + num2 + '_rnn_net_params1.pkl')

            self.win_rates.append(win_rate)
            self.episode_rewards.append(episode_reward)

            self.plt(6)
            print(win_rate)
            print(episode_reward)

            evaluate_steps += 1
            # if evaluate_steps > 0 and evaluate_steps % self.args.update2 == 0 and self.args.reuse_one == False:
            #     for agent in self.agents:#遗传思想：每self.args.update2次随机选较好的一组，更新所有agent。
            #         randi=random.randint(0, top-1)
            #         randi_index=top_10_indices[randi]
            #         agent.policy.target_rnn.load_state_dict(self.agents[randi_index].policy.eval_rnn.state_dict())
            #         randi=random.randint(0, half-1)
            #         randi_index=top_20_indices[randi]
            #         agent.policy.eval_rnn.load_state_dict(self.agents[randi_index].policy.eval_rnn.state_dict())
            # episode的每一项都是一个(1, episode_len, n_agents, 具体维度)四维数组，下面要把所有episode的的obs拼在一起
            # episode_batch = episodes[0]
            # episodes.pop(0)
            # for episode in episodes:
            #     for key in episode_batch.keys():
            #         episode_batch[key] = np.concatenate((episode_batch[key], episode[key]), axis=0)
            coo=0
            for episode_batch in tmp:
                if self.args.alg.find('coma') > -1 or self.args.alg.find('central_v') > -1 or self.args.alg.find('reinforce') > -1:
                    self.agents.train(episode_batch, train_steps, self.rolloutWorker.epsilon)
                    train_steps += 1
                else:
                    coo+=1

                    if coo % 2==1:
                        self.buffer.store_episode(episode_batch)
                    else:
                        self.buffer2.store_episode(episode_batch)
                    mini=[]
                    #mini_batch = self.buffer.sample(min(self.buffer.current_size, self.args.batch_size))
                    mini2=[]
                    if coo % 2==1:
                        mini_batch = self.buffer.sample(min(self.buffer.current_size, self.args.batch_size))
                        mini.append(mini_batch)
                    else:
                        mini_batch = self.buffer2.sample(min(self.buffer2.current_size, self.args.batch_size))
                        mini2.append(mini_batch)

                    # for train_step in range(self.args.train_steps):
                    threads=[]
                        
                    if coo % 2==1:
                        thread = threading.Thread(target=self.worker, args=(self.agents[0], mini[0], train_steps))
                    else:
                        thread = threading.Thread(target=self.worker, args=(self.agents[1], mini2[0], train_steps))
                    threads.append(thread)
                    thread.start()

                    # 等待所有线程完成
                    for thread in threads:
                        thread.join()
                    train_steps += 1
                print("train_steps {}".format(train_steps))
                # if self.args.same!=1:
                #     try:
                #         # 打开文件并追加写入新的JSON对象
                #         with open(self.save_path+'/data.json', 'a') as file:
                #             list_of_lists = {key: arr.tolist() for key, arr in episode_batch.items()}
                #             json.dump(list_of_lists, file)
                #             file.write('\n')  # 添加换行符，以便每个JSON对象占据单独一行
                #     except OSError as e:
                #         print(e)
            # win_rate, episode_reward = self.evaluate()
        # win_rate, episode_reward = self.evaluate()
        # print('win_rate is ', win_rate)
        # self.win_rates.append(win_rate)
        # self.episode_rewards.append(episode_reward)
        # self.plt(num)

    def evaluate(self):
        win_number = 0
        episode_rewards = 0
        for epoch in range(self.args.evaluate_epoch):
            _, episode_reward, win_tag, _ = self.rolloutWorker.generate_episode(epoch, evaluate=True)
            episode_rewards += episode_reward
            if win_tag:
                win_number += 1
        return win_number / self.args.evaluate_epoch, episode_rewards / self.args.evaluate_epoch

    def plt(self, num):
        plt.figure()
        plt.cla()

        # 绘制第一个子图：win_rates
        plt.subplot(2, 1, 1)
        plt.plot(range(len(self.win_rates)), self.win_rates, marker='o')  # 添加 marker='o' 绘制数据点
        plt.xlabel('step*{}'.format(self.args.evaluate_cycle), fontsize=10)
        plt.ylabel('mid_reward', fontsize=10)
        plt.xticks(rotation=45)  # 旋转横坐标标签

        # 绘制第二个子图：episode_rewards
        plt.subplot(2, 1, 2)
        plt.plot(range(len(self.episode_rewards)), self.episode_rewards, marker='o')  # 添加 marker='o' 绘制数据点
        plt.xlabel('step*{}'.format(self.args.evaluate_cycle), fontsize=10)
        plt.ylabel('episode_rewards', fontsize=10)
        plt.xticks(rotation=45)  # 旋转横坐标标签

        # 调整布局，避免重叠
        plt.tight_layout()

        # 保存图像和数据
        plt.savefig(self.save_path + '/plt_{}.png'.format(num), format='png')
        np.save(self.save_path + '/win_rates_{}'.format(num), self.win_rates)
        np.save(self.save_path + '/episode_rewards_{}'.format(num), self.episode_rewards)
        plt.close()










