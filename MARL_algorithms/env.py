import requests
import json
import time
import numpy as np
class Env:
    def __init__(self, tx):
        self.tx=tx
        self.port=8888
        pass
    def get_env_info(self):
        # 发起 GET 请求
        response = requests.get('http://localhost:{}/get_env_info'.format(self.port))

        # 检查响应状态码
        if response.status_code == 200:
            # 打印响应内容
            try:
                return json.loads(response.text)
            except:
                print(response.text)
                return response.text

        else:
            print('Failed to fetch data:', response.status_code)
            return json.loads('{"name": "John", "age": 30, "city": "New York"}')

    #mod
    def get_obs(self):
        # 发起 GET 请求
        headers = {'User-Agent': 'Mozilla/5.0'}
        response = requests.post('http://localhost:{}/get_obs'.format(self.port), headers=headers)
        self.tx = response.text

        # 检查响应状态码
        if response.status_code == 200:
            # 解析 JSON 响应内容
            data_dict = json.loads(response.text)
            matrix = [[float(data_dict[str(i)][str(j)]) for j in range(len(data_dict[str(i)]))] for i in
                      range(len(data_dict))]

            # 获取第 0 行和其他行
            signal_obs = matrix[0]
            car_obs = matrix[1:]  # 从第 1 行开始到结尾
            car_obs = np.array(car_obs)
            car_obs = car_obs[:, -4:]
            # 将 first_row 和 other_rows 转换为 JSON 字符串
            self.tx = json.dumps({
                "first_row": signal_obs.tolist() if isinstance(signal_obs, np.ndarray) else signal_obs,
                "other_rows": car_obs.tolist() if isinstance(car_obs, np.ndarray) else car_obs
            })
            # 返回两个变量
            return signal_obs, car_obs
        else:
            print('Failed to fetch data:', response.status_code)
            return [], json.loads('[1, 2, 3, 4, 5]')

    def get_state(self):
        # 将 JSON 字符串转换为字典
        tx_data = json.loads(self.tx)
        # 返回 `other_rows` 的第一个元素
        return tx_data["other_rows"]

    def step(self, actions,actions_sig):
        params={
            "actions":json.dumps(actions),
            "actions_sig":json.dumps(actions_sig)
        }
        # 发起 GET 请求
        response = requests.get('http://localhost:{}/step'.format(self.port), params=params)
        inf={
            "battle_won":False
        }
        rre=json.loads(response.text)
        time.sleep(1)
        response = requests.get('http://localhost:{}/get_re'.format(self.port))
        rre=json.loads(response.text)
        term=False
        if float(rre["1"])==1:
            term=True
        # 检查响应状态码mod
        if response.status_code == 200:
            lis=[float(rre[str(i)]) for i in range(len(rre))]
            tol_re=lis[0]
            del lis[0]
            del lis[0]
            return tol_re,lis,term,inf
        else:
            print('Failed to fetch data:', response.status_code)
            return 0,False,inf
    def reset(self):
        # 发起 GET 请求
        response = requests.get('http://localhost:{}/reset'.format(self.port))
        i=0
        while True:
            if(i>5):
                break
            try:
                response = requests.get('http://localhost:{}/get_env_info'.format(self.port))
                if response.status_code == 200:
                    break
            except requests.ConnectionError as e:
                print("Connection error:", e)
            i=i+1
            time.sleep(2)
        requests.get('http://localhost:{}/go'.format(self.port))
        # 检查响应状态码
        if response.status_code == 200:
            # 打印响应内容
            # print(response.text)
            return response.text
        else:
            print('Failed to fetch data:', response.status_code)
            return "damn"
    def close(self):
        # 发起 GET 请求
        response = requests.get('http://localhost:{}/close'.format(self.port))

        # 检查响应状态码
        if response.status_code == 200:
            # 打印响应内容
            # print(response.text)
            return response.text
        else:
            print('Failed to fetch data:', response.status_code)
            return "damn"
    def get_avail_agent_actions(self, agent_id):
        # 发起 GET 请求
        # response = requests.get('http://localhost:8888/avail')

        # # 检查响应状态码
        # if response.status_code == 200:
            # 打印响应内容
            # print(response.text)
            # data_dict2=json.loads(response.text)
            # matrix2 = [[bool(int(data_dict2[str(i)][str(j)])) for j in range(len(data_dict2[str(i)]))] for i in range(len(data_dict2))]
            # if True not in matrix2[agent_id]:
            #     return [False, False, True, False]
            # return matrix2[agent_id]
        # else:
        #     print('Failed to fetch data:', response.status_code)
        #     return [False, False, True, False]
        return [True, True,True,True]
    def save_replay(self):
        return 0
