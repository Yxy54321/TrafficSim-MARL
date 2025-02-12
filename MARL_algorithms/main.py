from runner import Runner
from agent.agent import Agents
from env import Env
from common.arguments import get_common_args, get_coma_args, get_mixer_args, get_centralv_args, get_reinforce_args, get_commnet_args, get_g2anet_args
import pickle
import matplotlib.pyplot as plt
import sys
import numpy as np

def copy_pickle(input_file, output_file):
    with open(input_file, 'rb') as f:
        data = f.read()

    with open(output_file, 'wb') as f:
        f.write(data)

class agentArgs:
    def __init__(self,args, n_actions=0, n_agents=0, state_shape=0,obs_shape=0,buffer_size=0,episode_limit=0):
        self.n_actions = n_actions
        self.n_agents = n_agents
        self.state_shape = state_shape
        self.obs_shape = obs_shape
        self.buffer_size = buffer_size
        self.episode_limit = episode_limit
        self.alg='iql'
        self.rnn_hidden_dim=64
        self.last_action = True
        self.reuse_network = True
        self.load_model = False
        self.cuda=True
        self.model_dir = "./model/signal/"
        self.map = args.map
        self.optimizer=args.optimizer
        self.lr=args.lr


if __name__ == '__main__':
    #每个模型都试一遍？
    algA=['iql','vdn', 'qmix', 'coma', 'qtran_base','qtran_alt', 'reinforce+commnet','reinforce+g2anet', 'maven']
    for i in range(8):
        args = get_common_args()
        args.alg=algA[i]
        if args.alg.find('coma') > -1:
            args = get_coma_args(args)
        elif args.alg.find('central_v') > -1:
            args = get_centralv_args(args)
        elif args.alg.find('reinforce') > -1:
            args = get_reinforce_args(args)
        else:
            args = get_mixer_args(args)
        if args.alg.find('commnet') > -1:
            args = get_commnet_args(args)
        if args.alg.find('g2anet') > -1:
            args = get_g2anet_args(args)
        env=Env(algA[i])
        env_info = env.get_env_info()
        #episode lim 360  n_actions:4 n_agents:3 obs_shape:22 state_shape:22
        # print("hhh")
        print(env_info)
        #什么含义?
        #mod
        if args.signal:
            args.signal_args=agentArgs(args)
            args.signal_args.n_agents=env_info["n_signal_agents"]
            args.signal_args.n_actions=env_info["n_signal_actions"]
            args.signal_args.state_shape=env_info["signal_state_shape"]
            args.signal_args.obs_shape=env_info["signal_obs_shape"]
            args.signal_args.buffer_size=args.buffer_size
            args.signal_args.episode_limit =env_info["episode_limit"]

        args.same=env_info["n_agents"]
        args.n_actions = env_info["n_actions"]
        #agent的种类数，即实际agent的个数
        '''
        todo:env_info里需要一个列表，[same0,same1,....] len=args.n_agents
                                   car  signal
        '''

        # to : args.n_agents=len(env_info["n_agents"]),env_info["n_agents"]为一个list
        args.n_agents = int(env_info["n_agents"]/args.same)
        args.state_shape = env_info["state_shape"]
        args.obs_shape = env_info["obs_shape"]
        args.episode_limit = env_info["episode_limit"]
        args.evaluate_cycle=env_info["episode_limit"]*10
        
        # j=1
        # episode_rewards=[]
        # while True:
        #     if j>38:
        #         break
        #     copy_pickle("./model/iql/smart_city/"+str(j)+"_rnn_net_params.pkl", "./model/iql/smart_city/rnn_net_params.pkl")
        #     args.load_model=True
        #     runner = Runner(env, args)
        #     win_rate, rewards = runner.evaluate()
        #     print('The rewards of {} is  {}'.format(j,rewards))
        #     episode_rewards.append(rewards)
        #     j=j+1
        # plt.figure()
        # plt.cla()
        # plt.plot(range(len(episode_rewards)), episode_rewards)
        # plt.xlabel('episode_step*8')
        # plt.ylabel('episode_rewards')
        # plt.savefig('./result/iql/smart_city/plt_evalute.png', format='png')
        # plt.close()
        # break

        if not args.evaluate:
            runner = Runner(env, args)
            runner.run(i)
        else:
            agents=Agents(args)

            for i in range(args.evaluate_epoch):
                env.reset()
                agents.policy.init_hidden(1)
                step=0
                last_action= np.zeros((args.n_agents, args.n_actions))
                episode_reward=0
                while step < args.episode_limit:
                # time.sleep(0.2)
                    obs = env.get_obs()
                    state = env.get_state()
                    actions, avail_actions, actions_onehot = [], [], []
                    for agent_id in range(args.n_agents):
                        avail_action = env.get_avail_agent_actions(agent_id)
                        action = agents.choose_action(obs[agent_id], last_action[agent_id], agent_id,
                                                            avail_action, 0)
                        action_onehot = np.zeros(args.n_actions)
                        action_onehot[action] = 1
                        actions.append(int(action))
                        actions_onehot.append(action_onehot)
                        avail_actions.append(avail_action)
                        last_action[agent_id] = action_onehot
                        # generate onehot vector of th action

                    reward, terminated, info = env.step(actions)
                    episode_reward+=reward[0]
                    step+=1
                print('The reward of {} is  {}'.format(args.alg, episode_reward))
            break
        env.close()
